using LiveChartsCore.Geo;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.SKCharts;

namespace SnapshotTests;

[TestClass]
public sealed class MapsTests
{
    private static HeatLandSeries[] CreateHeatSeries() =>
    [
        new()
        {
            Lands =
            [
                new() { Name = "bra", Value = 13 },
                new() { Name = "mex", Value = 10 },
                new() { Name = "usa", Value = 15 },
                new() { Name = "can", Value = 8 },
                new() { Name = "ind", Value = 12 },
                new() { Name = "deu", Value = 13 },
                new() { Name = "jpn", Value = 15 },
                new() { Name = "chn", Value = 14 },
                new() { Name = "rus", Value = 11 },
                new() { Name = "fra", Value = 8 },
                new() { Name = "esp", Value = 7 },
                new() { Name = "kor", Value = 10 },
                new() { Name = "zaf", Value = 12 },
                new() { Name = "are", Value = 13 }
            ]
        }
    ];

    [TestMethod]
    public void Basic()
    {
        var chart = new SKGeoMap
        {
            Series = CreateHeatSeries(),
            MapProjection = MapProjection.Mercator,
            Width = 600,
            Height = 600
        };

        chart.AssertSnapshotMatches($"{nameof(MapsTests)}_{nameof(Basic)}");
    }

    [TestMethod]
    public void OrthographicDefault()
    {
        var chart = new SKGeoMap
        {
            Series = CreateHeatSeries(),
            MapProjection = MapProjection.Orthographic,
            Width = 600,
            Height = 600
        };

        chart.AssertSnapshotMatches($"{nameof(MapsTests)}_{nameof(OrthographicDefault)}");
    }

    [TestMethod]
    public void OrthographicRotated()
    {
        var chart = new SKGeoMap
        {
            Series = CreateHeatSeries(),
            MapProjection = MapProjection.Orthographic,
            Width = 600,
            Height = 600
        };

        // Rotate to show Europe/Africa centered view
        chart.CoreChart.RotationX = 15;
        chart.CoreChart.RotationY = 20;

        chart.AssertSnapshotMatches($"{nameof(MapsTests)}_{nameof(OrthographicRotated)}");
    }
}
