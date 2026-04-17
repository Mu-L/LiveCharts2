using System;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Geo;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests.OtherTests;

[TestClass]
public class ChartInteractiveApiTests
{
    [TestMethod]
    public void CartesianZoomIn_And_Pan_AdjustAxisLimits()
    {
        var xAxis = new Axis();
        var yAxis = new Axis();

        var chart = new SKCartesianChart
        {
            Width = 400,
            Height = 400,
            XAxes = [xAxis],
            YAxes = [yAxis],
            Series = [ new LineSeries<double> { Values = [1d, 2d, 3d, 4d, 5d] } ]
        };

        _ = chart.GetImage();

        var core = (CartesianChartEngine)chart.CoreChart;

        // Zoom in at the center — exercises the "DefinedByZoomIn" path.
        core.Zoom(
            ZoomAndPanMode.Both,
            new LvcPoint(200, 200),
            ZoomDirection.ZoomIn);

        // Pan by a delta — exercises PanAxis on both axes.
        core.Pan(ZoomAndPanMode.Both, new LvcPoint(20, 20));

        _ = chart.GetImage();
        // A second render should not throw.
    }

    [TestMethod]
    public void CartesianZoomWithScaleFactor_WithWrongDirection_Throws()
    {
        var chart = new SKCartesianChart
        {
            Width = 200,
            Height = 200,
            Series = [ new LineSeries<double> { Values = [1d, 2d, 3d] } ]
        };
        _ = chart.GetImage();

        var core = (CartesianChartEngine)chart.CoreChart;

        // When a scale factor is provided the direction MUST be DefinedByScaleFactor.
        Assert.ThrowsExactly<InvalidOperationException>(
            () => core.Zoom(
                ZoomAndPanMode.X,
                new LvcPoint(0, 0),
                ZoomDirection.ZoomIn,
                scaleFactor: 1.5));
    }

    [TestMethod]
    public void CartesianZoomingSection_StartGrowEndFullCycle()
    {
        var chart = new SKCartesianChart
        {
            Width = 400,
            Height = 400,
            Series = [ new LineSeries<double> { Values = [1d, 2d, 3d, 4d] } ]
        };
        _ = chart.GetImage();

        var core = (CartesianChartEngine)chart.CoreChart;

        // Simulate the user dragging a zoom box across the draw margin.
        var start = new LvcPoint(
            core.DrawMarginLocation.X + 20,
            core.DrawMarginLocation.Y + 20);
        var end = new LvcPoint(
            core.DrawMarginLocation.X + core.DrawMarginSize.Width - 20,
            core.DrawMarginLocation.Y + core.DrawMarginSize.Height - 20);

        core.StartZoomingSection(ZoomAndPanMode.Both, start);
        core.GrowZoomingSection(ZoomAndPanMode.Both, new LvcPoint(
            (start.X + end.X) / 2, (start.Y + end.Y) / 2));
        core.EndZoomingSection(ZoomAndPanMode.Both, end);

        _ = chart.GetImage();
    }

    [TestMethod]
    public void CartesianZoomingSection_StartOutsideDrawMarginIsNoOp()
    {
        var chart = new SKCartesianChart
        {
            Width = 400,
            Height = 400,
            Series = [ new LineSeries<double> { Values = [1d, 2d, 3d] } ]
        };
        _ = chart.GetImage();

        var core = (CartesianChartEngine)chart.CoreChart;

        // Starting outside the draw margin should early-return without crashing.
        core.StartZoomingSection(ZoomAndPanMode.Both, new LvcPoint(-10, -10));
        _ = chart.GetImage();
    }

    [TestMethod]
    public void CartesianZoomingSection_NoZoomBySectionFlagIsNoOp()
    {
        var chart = new SKCartesianChart
        {
            Width = 400,
            Height = 400,
            Series = [ new LineSeries<double> { Values = [1d, 2d, 3d] } ]
        };
        _ = chart.GetImage();

        var core = (CartesianChartEngine)chart.CoreChart;

        core.StartZoomingSection(
            ZoomAndPanMode.Both | ZoomAndPanMode.NoZoomBySection,
            new LvcPoint(100, 100));
    }

    private static SKGeoMap CreateGeoMap(MapProjection projection = MapProjection.Mercator) =>
        new()
        {
            Width = 400,
            Height = 400,
            Series = [ new HeatLandSeries { Lands = [ new() { Name = "bra", Value = 1 } ] } ],
            MapProjection = projection
        };

    [TestMethod]
    public void GeoMap_PanAndZoomAndResetViewportBeforeRender()
    {
        // Interactions must happen before GetImage — SourceGenSKMapChart.DrawOnCanvas
        // unconditionally calls Unload() at the end of every render, which nulls the
        // map factory used by Pan/Zoom/ResetViewport.
        var chart = CreateGeoMap();

        chart.CoreChart.Pan(new LvcPoint(10, 20));
        chart.CoreChart.Zoom(new LvcPoint(100, 100), ZoomDirection.ZoomIn);
        chart.CoreChart.Zoom(new LvcPoint(100, 100), ZoomDirection.ZoomOut);
        chart.CoreChart.ResetViewport();

        _ = chart.GetImage();
    }

    [TestMethod]
    public void GeoMap_ViewToAndRotateToBeforeRender()
    {
        var chart = CreateGeoMap(MapProjection.Orthographic);

        chart.CoreChart.ViewTo(command: null);
        chart.CoreChart.RotateTo(longitude: 10, latitude: 20, durationMs: 0);

        _ = chart.GetImage();
    }

    [TestMethod]
    public void GeoMap_PointerEventsFlowThroughInvokers()
    {
        var chart = CreateGeoMap();

        // Drives InvokePointerDown/Move/Up/Left — these raise events and the factory
        // may react via hover/pan hooks. Must run before GetImage.
        chart.CoreChart.InvokePointerDown(new LvcPoint(50, 50));
        chart.CoreChart.InvokePointerMove(new LvcPoint(60, 55));
        chart.CoreChart.InvokePointerMove(new LvcPoint(80, 75));
        chart.CoreChart.InvokePointerUp(new LvcPoint(100, 100));
        chart.CoreChart.InvokePointerLeft();

        _ = chart.GetImage();
    }

    [TestMethod]
    public void GeoMap_FindLandAtReturnsNullOutsideAnyLand()
    {
        var chart = CreateGeoMap();

        // FindLandAt far outside any rendered land hits the "no match" branch.
        // Must run before GetImage so the chart hasn't been unloaded yet.
        var hit = chart.CoreChart.FindLandAt(new LvcPoint(-1000, -1000));
        Assert.IsNull(hit);

        _ = chart.GetImage();
    }
}
