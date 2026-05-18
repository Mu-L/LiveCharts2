using System.Linq;
using LiveChartsCore.Drawing;
using LiveChartsCore.Geo;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests.ChartTests;

[TestClass]
public class GeoMapTests
{
    // https://github.com/Live-Charts/LiveCharts2/issues/962
    //
    // Swapping the Series collection on a GeoMap should preserve the heat fill
    // on lands that exist in both the old and the new series. Before the fix,
    // GeoMapChart.Measure() painted the new series first and then called
    // Delete() on the departed series, which nulled Shape.Fill on every land
    // the old series had ever painted -- including ones the new series just
    // painted -- so shared lands appeared blank until the next measure.
    [TestMethod]
    public void GeoMap_SwappingSeries_PreservesFillOnSharedLands()
    {
        var chart = new SKGeoMap
        {
            Width = 400,
            Height = 400,
            Series = [
                new HeatLandSeries
                {
                    Lands = [
                        new() { Name = "fra", Value = 10 },
                        new() { Name = "usa", Value = 5 },
                    ]
                }
            ]
        };

        chart.CoreChart.Measure();

        var fra = chart.ActiveMap.FindLand("fra");
        var usa = chart.ActiveMap.FindLand("usa");
        Assert.IsNotNull(fra, "fra should exist in the world map");
        Assert.IsNotNull(usa, "usa should exist in the world map");
        Assert.IsTrue(
            fra.Data.All(d => d.Shape?.Fill is not null),
            "fra should be painted after the first measure");

        // Swap to a series that still contains "fra" but drops "usa".
        chart.Series = [
            new HeatLandSeries
            {
                Lands = [
                    new() { Name = "fra", Value = 1 },
                    new() { Name = "bra", Value = 99 },
                ]
            }
        ];

        chart.CoreChart.Measure();

        Assert.IsTrue(
            fra.Data.All(d => d.Shape?.Fill is not null),
            "fra is shared between the old and new series and must stay painted after swap (#962)");
        Assert.IsTrue(
            usa.Data.All(d => d.Shape?.Fill is null),
            "usa is no longer in any series and must be cleared after swap");
    }

    // Regression for the tooltip-anchor fix in PR #2251.
    //
    // Russia in world.geojson is a MultiPolygon: one mainland ring at lon ≈
    // [27.29, 180] plus several Chukotka rings wrapped to lon ≈ [-180, -169.9].
    // Before the fix, ComputeLandScreenCenter took a single bbox across every
    // contour — the union spans the entire map width and its centroid lands
    // mid-Atlantic on Mercator. After the fix, the largest visible contour wins
    // and the centroid lands inside mainland Russia, where users expect it.
    [TestMethod]
    public void GeoMap_TooltipAnchor_PicksLargestContour_NotUnionBbox()
    {
        const float Width = 600f, Height = 600f;

        var chart = new SKGeoMap
        {
            Width = (int)Width,
            Height = (int)Height,
            MapProjection = MapProjection.Mercator,
            Series = [new HeatLandSeries { Lands = [new() { Name = "rus", Value = 10 }] }]
        };

        chart.CoreChart.Measure();

        var russia = chart.ActiveMap.FindLand("rus");
        Assert.IsNotNull(russia);
        Assert.IsTrue(
            russia.Data.Length > 1,
            "rus is expected to be multi-contour in world.geojson; the regression depends on it");

        // Hover at Moscow (lon=37.62, lat=55.75), projected with the same
        // projector the chart uses for measurement.
        var projector = Maps.BuildProjector(MapProjection.Mercator, [Width, Height]);
        projector.ToMap(37.62, 55.75, out var moscowX, out var moscowY);

        var hit = chart.CoreChart.FindLandAt(new LvcPoint(moscowX, moscowY));
        Assert.IsNotNull(hit, "Moscow must hit the rus LandDefinition");
        Assert.AreSame(russia, hit.Value.Land);

        var center = hit.Value.Center;
        var anchorInsideContour = russia.Data
            .Any(d => d.Shape?.ContainsPoint(center.X, center.Y) ?? false);
        Assert.IsTrue(
            anchorInsideContour,
            $"Tooltip anchor ({center.X:0.0}, {center.Y:0.0}) must fall inside a Russia contour; " +
            "before the fix the union-bbox centroid landed in the Atlantic.");
    }
}
