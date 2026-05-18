using System.Collections.Generic;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Geo;
using LiveChartsCore.Kernel;
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

    // The map participates in the IChartView pointer-event surface: a click on
    // a land must fire DataPointerDown with a ChartPoint whose DataSource is the
    // LandDefinition. Before this wiring, the map only fired its own bespoke
    // LandClicked event and the standard IChartView events stayed silent.
    [TestMethod]
    public void GeoMap_PointerUpOnLand_FiresDataPointerDownWithLandDefinition()
    {
        const float Width = 600f, Height = 600f;

        var chart = new SKGeoMap
        {
            Width = (int)Width,
            Height = (int)Height,
            MapProjection = MapProjection.Mercator,
        };
        chart.CoreChart.Measure();

        var projector = Maps.BuildProjector(MapProjection.Mercator, [Width, Height]);
        projector.ToMap(37.62, 55.75, out var moscowX, out var moscowY);
        var clickPoint = new LvcPoint(moscowX, moscowY);

        ChartPoint[]? capturedPoints = null;
        LvcPoint? capturedPointer = null;
        chart.DataPointerDown += (sender, points) =>
        {
            capturedPoints = points.ToArray();
            // sender is the view; surface that to assert it round-trips.
            capturedPointer = clickPoint;
        };

        chart.CoreChart.InvokePointerDown(clickPoint, isSecondaryAction: false);
        chart.CoreChart.InvokePointerUp(clickPoint, isSecondaryAction: false);

        Assert.IsNotNull(capturedPoints, "DataPointerDown must fire on a land click");
        Assert.AreEqual(1, capturedPoints.Length, "geo click emits exactly one ChartPoint (one land hit)");
        Assert.IsNotNull(capturedPointer);
        var land = capturedPoints[0].Context.DataSource as LandDefinition;
        Assert.IsNotNull(land, "Context.DataSource must be the LandDefinition");
        Assert.AreEqual("rus", land.ShortName, "Moscow's land short-name is 'rus'");
        Assert.IsNotNull(capturedPoints[0].Context.HoverArea, "land hits carry a hover area for the screen bbox");
    }

    // A drag (pointer-down → move >5px → up) must NOT fire DataPointerDown —
    // matches the existing _pointerDownIsClick deadzone that gated LandClicked.
    [TestMethod]
    public void GeoMap_DragGesture_DoesNotFireDataPointerDown()
    {
        var chart = new SKGeoMap
        {
            Width = 600,
            Height = 600,
            MapProjection = MapProjection.Mercator,
        };
        chart.CoreChart.Measure();

        var projector = Maps.BuildProjector(MapProjection.Mercator, [600f, 600f]);
        projector.ToMap(37.62, 55.75, out var x, out var y);

        var fired = false;
        chart.DataPointerDown += (_, _) => fired = true;

        chart.CoreChart.InvokePointerDown(new LvcPoint(x, y), isSecondaryAction: false);
        // Move > sqrt(25) = 5 px so the click intent is cleared.
        chart.CoreChart.InvokePointerMove(new LvcPoint(x + 20, y + 20));
        chart.CoreChart.InvokePointerUp(new LvcPoint(x + 20, y + 20), isSecondaryAction: false);

        Assert.IsFalse(fired, "moving more than the click-deadzone must suppress DataPointerDown");
    }

    // The hover throttler must fire HoveredPointsChanged on enter, again with a
    // distinct (new, old) pair on a transition to another land, and once more
    // with (null, old) when the pointer leaves the chart entirely.
    [TestMethod]
    public void GeoMap_HoverTransitions_FireHoveredPointsChanged()
    {
        const float Width = 800f, Height = 800f;
        var chart = new SKGeoMap
        {
            Width = (int)Width,
            Height = (int)Height,
            MapProjection = MapProjection.Mercator,
        };
        chart.CoreChart.Measure();

        var projector = Maps.BuildProjector(MapProjection.Mercator, [Width, Height]);
        projector.ToMap(2.35, 48.85, out var parisX, out var parisY);          // fra
        projector.ToMap(13.40, 52.52, out var berlinX, out var berlinY);       // deu

        var calls = new List<(IEnumerable<ChartPoint>? n, IEnumerable<ChartPoint>? o)>();
        chart.HoveredPointsChanged += (_, n, o) => calls.Add((n, o));

        // Enter France
        chart.CoreChart.InvokePointerMove(new LvcPoint(parisX, parisY));
        // The tooltip throttler debounces 50ms; force it on the test thread.
        RunPendingTooltip(chart);

        // Move to Germany
        chart.CoreChart.InvokePointerMove(new LvcPoint(berlinX, berlinY));
        RunPendingTooltip(chart);

        // Leave the chart
        chart.CoreChart.InvokePointerLeft();

        Assert.AreEqual(3, calls.Count, "expected enter, transition, exit");

        var enterLand = calls[0].n!.First().Context.DataSource as LandDefinition;
        Assert.IsNull(calls[0].o);
        Assert.AreEqual("fra", enterLand!.ShortName);

        var transitionNew = calls[1].n!.First().Context.DataSource as LandDefinition;
        var transitionOld = calls[1].o!.First().Context.DataSource as LandDefinition;
        Assert.AreEqual("deu", transitionNew!.ShortName);
        Assert.AreEqual("fra", transitionOld!.ShortName);

        Assert.IsNull(calls[2].n);
        Assert.AreEqual("deu", (calls[2].o!.First().Context.DataSource as LandDefinition)!.ShortName);
    }

    // GetPointsAt is the synchronous lookup variant of the click pipeline: it
    // returns whatever DataPointerDown WOULD fire for the same screen point.
    [TestMethod]
    public void GeoMap_GetPointsAt_ReturnsHitLand()
    {
        var chart = new SKGeoMap
        {
            Width = 600,
            Height = 600,
            MapProjection = MapProjection.Mercator,
        };
        chart.CoreChart.Measure();

        var projector = Maps.BuildProjector(MapProjection.Mercator, [600f, 600f]);
        projector.ToMap(37.62, 55.75, out var x, out var y);

        var points = chart.GetPointsAt(new LvcPointD(x, y)).ToArray();

        Assert.AreEqual(1, points.Length);
        Assert.AreEqual("rus", (points[0].Context.DataSource as LandDefinition)!.ShortName);

        // Ocean — no hit.
        Assert.AreEqual(0, chart.GetPointsAt(new LvcPointD(1, 1)).Count());
    }

    // GeoMapChart owns its own _tooltipThrottler (50ms ActionThrottler); the
    // tests need a deterministic way to flush it. The throttler dispatches its
    // work via View.InvokeOnUIThread which on SKGeoMap runs synchronously, so
    // calling InvokePointerMove + sleeping the throttler interval is enough.
    private static void RunPendingTooltip(SKGeoMap chart)
    {
        // The 50ms throttle interval is implementation detail; call the
        // throttler directly via reflection to avoid wall-clock waits in tests.
        var throttler = typeof(GeoMapChart)
            .GetField("_tooltipThrottler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            !.GetValue(chart.CoreChart);
        var unlocked = typeof(GeoMapChart)
            .GetMethod("TooltipThrottlerUnlocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = (System.Threading.Tasks.Task)unlocked!.Invoke(chart.CoreChart, null)!;
        task.Wait();
        _ = throttler; // silence unused
    }
}
