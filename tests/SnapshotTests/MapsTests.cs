using LiveChartsCore.Drawing;
using LiveChartsCore.Geo;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.SKCharts;
using LiveChartsCore.SkiaSharpView.VisualElements;
using SkiaSharp;

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

    // Regression for the orthographic horizon-arc fix in PR #2251.
    //
    // Centered over Central Asia, Russia/China/India straddle the horizon and
    // exercise BuildOrthographicPath's exit/entry handling. Before the fix,
    // path.Close() drew a chord between the last horizon-exit and the first
    // horizon-entry, slicing through the visible disc and producing visible
    // bulges past the rim. After the fix, EmitHorizonArc walks the disc
    // boundary in 3° steps and the silhouette stays clean.
    [TestMethod]
    public void OrthographicHorizonClipsAlongDiscRim()
    {
        var chart = new SKGeoMap
        {
            Series = CreateHeatSeries(),
            MapProjection = MapProjection.Orthographic,
            Width = 600,
            Height = 600
        };

        chart.CoreChart.RotationX = 80;
        chart.CoreChart.RotationY = 20;

        chart.AssertSnapshotMatches($"{nameof(MapsTests)}_{nameof(OrthographicHorizonClipsAlongDiscRim)}");
    }

    // Mirrors the Avalonia World sample: Title above the map + Right-anchored
    // SKHeatLegend. Locks both the title position and the legend gradient
    // rendering against the layout reservation done by GeoMapChart.Measure.
    [TestMethod]
    public void BasicWithTitleAndLegend()
    {
        var chart = new SKGeoMap
        {
            Series = CreateHeatSeries(),
            MapProjection = MapProjection.Mercator,
            Width = 800,
            Height = 600,
            LegendPosition = LegendPosition.Right,
            Legend = new SKHeatLegend(),
            Title = new DrawnLabelVisual(new LabelGeometry
            {
                Text = "World population by country",
                TextSize = 20,
                Padding = new Padding(12),
                Paint = new SolidColorPaint(SKColors.Black),
            }),
        };

        chart.AssertSnapshotMatches($"{nameof(MapsTests)}_{nameof(BasicWithTitleAndLegend)}");
    }

    // The Default projection treats geo coordinates as raw equirectangular
    // (lon -180..180 → 0..w, lat 90..-90 → 0..h) with no Mercator stretching
    // and no latitude clip. Continents render in their unprojected aspect.
    [TestMethod]
    public void DefaultProjection()
    {
        var chart = new SKGeoMap
        {
            Series = CreateHeatSeries(),
            MapProjection = MapProjection.Default,
            Width = 600,
            Height = 600
        };

        chart.AssertSnapshotMatches($"{nameof(MapsTests)}_{nameof(DefaultProjection)}");
    }

    // Frames the map on Europe via the lat/lon bounds API. Locks that the
    // bounds drive both the projection's scale (Iceland..Caucasus fills the
    // width) and the canvas-clip rect (lands outside the bounds are pixel-
    // clipped instead of bleeding into the canvas padding).
    [TestMethod]
    public void MercatorEuropeView()
    {
        var chart = new SKGeoMap
        {
            Series = CreateHeatSeries(),
            MapProjection = MapProjection.Mercator,
            Width = 600,
            Height = 600,
            // Europe: ~Iceland (lon -25°) east to the Caucasus (+45°),
            // ~North Africa coast (lat 35°) north to North Cape (72°).
            MinLatitude = 35,
            MaxLatitude = 72,
            MinLongitude = -25,
            MaxLongitude = 45,
        };

        chart.AssertSnapshotMatches($"{nameof(MapsTests)}_{nameof(MercatorEuropeView)}");
    }
}
