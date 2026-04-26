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

    private static (SKCartesianChart chart, Axis xAxis, Axis yAxis, CartesianChartEngine core) CreatePinnedChart()
    {
        // MinLimit/MaxLimit are pinned so Zoom/Pan operations leave a measurable change
        // on the axis limits without the auto-fit logic snapping things back.
        var xAxis = new Axis { MinLimit = 0, MaxLimit = 10 };
        var yAxis = new Axis { MinLimit = 0, MaxLimit = 10 };

        var chart = new SKCartesianChart
        {
            Width = 400,
            Height = 400,
            XAxes = [xAxis],
            YAxes = [yAxis],
            Series = [ new LineSeries<double> { Values = [1d, 2d, 3d, 4d, 5d, 6d, 7d, 8d, 9d, 10d] } ]
        };

        _ = chart.GetImage();
        var core = (CartesianChartEngine)chart.CoreChart;
        return (chart, xAxis, yAxis, core);
    }

    [TestMethod]
    public void Zoom_WithZoomXOnly_AffectsOnlyXAxis()
    {
        var (_, xAxis, yAxis, core) = CreatePinnedChart();

        var xRangeBefore = xAxis.MaxLimit!.Value - xAxis.MinLimit!.Value;
        var yRangeBefore = yAxis.MaxLimit!.Value - yAxis.MinLimit!.Value;

        core.Zoom(ZoomAndPanMode.ZoomX | ZoomAndPanMode.NoFit, new LvcPoint(200, 200), ZoomDirection.ZoomIn);

        var xRangeAfter = xAxis.MaxLimit!.Value - xAxis.MinLimit!.Value;
        var yRangeAfter = yAxis.MaxLimit!.Value - yAxis.MinLimit!.Value;

        Assert.IsTrue(xRangeAfter < xRangeBefore, "ZoomX should shrink the X axis range.");
        Assert.AreEqual(yRangeBefore, yRangeAfter, 1e-9, "ZoomX must leave the Y axis untouched.");
    }

    [TestMethod]
    public void Zoom_WithPanXOnly_DoesNotZoomXAxis()
    {
        // PanX alone enables panning but NOT zooming; a Zoom call should be a no-op.
        var (_, xAxis, yAxis, core) = CreatePinnedChart();

        var xRangeBefore = xAxis.MaxLimit!.Value - xAxis.MinLimit!.Value;
        var yRangeBefore = yAxis.MaxLimit!.Value - yAxis.MinLimit!.Value;

        core.Zoom(ZoomAndPanMode.PanX | ZoomAndPanMode.NoFit, new LvcPoint(200, 200), ZoomDirection.ZoomIn);

        Assert.AreEqual(xRangeBefore, xAxis.MaxLimit!.Value - xAxis.MinLimit!.Value, 1e-9);
        Assert.AreEqual(yRangeBefore, yAxis.MaxLimit!.Value - yAxis.MinLimit!.Value, 1e-9);
    }

    [TestMethod]
    public void Pan_WithPanXOnly_AffectsOnlyXAxis()
    {
        var (_, xAxis, yAxis, core) = CreatePinnedChart();

        var xMinBefore = xAxis.MinLimit!.Value;
        var yMinBefore = yAxis.MinLimit!.Value;

        core.Pan(ZoomAndPanMode.PanX | ZoomAndPanMode.NoFit, new LvcPoint(20, 20));

        Assert.AreNotEqual(xMinBefore, xAxis.MinLimit!.Value, "PanX should shift the X axis.");
        Assert.AreEqual(yMinBefore, yAxis.MinLimit!.Value, 1e-9, "PanX must leave the Y axis untouched.");
    }

    [TestMethod]
    public void Pan_WithZoomXOnly_DoesNotPanXAxis()
    {
        // ZoomX alone enables zoom but NOT pan; a Pan call should be a no-op.
        var (_, xAxis, yAxis, core) = CreatePinnedChart();

        var xMinBefore = xAxis.MinLimit!.Value;
        var yMinBefore = yAxis.MinLimit!.Value;

        core.Pan(ZoomAndPanMode.ZoomX | ZoomAndPanMode.NoFit, new LvcPoint(20, 20));

        Assert.AreEqual(xMinBefore, xAxis.MinLimit!.Value, 1e-9);
        Assert.AreEqual(yMinBefore, yAxis.MinLimit!.Value, 1e-9);
    }

    [TestMethod]
    public void Zoom_WithZoomYOnly_AffectsOnlyYAxis()
    {
        var (_, xAxis, yAxis, core) = CreatePinnedChart();

        var xRangeBefore = xAxis.MaxLimit!.Value - xAxis.MinLimit!.Value;
        var yRangeBefore = yAxis.MaxLimit!.Value - yAxis.MinLimit!.Value;

        core.Zoom(ZoomAndPanMode.ZoomY | ZoomAndPanMode.NoFit, new LvcPoint(200, 200), ZoomDirection.ZoomIn);

        Assert.AreEqual(xRangeBefore, xAxis.MaxLimit!.Value - xAxis.MinLimit!.Value, 1e-9);
        Assert.IsTrue(yAxis.MaxLimit!.Value - yAxis.MinLimit!.Value < yRangeBefore);
    }

    [TestMethod]
    public void Pan_WithPanYOnly_AffectsOnlyYAxis()
    {
        var (_, xAxis, yAxis, core) = CreatePinnedChart();

        var xMinBefore = xAxis.MinLimit!.Value;
        var yMinBefore = yAxis.MinLimit!.Value;

        core.Pan(ZoomAndPanMode.PanY | ZoomAndPanMode.NoFit, new LvcPoint(20, 20));

        Assert.AreEqual(xMinBefore, xAxis.MinLimit!.Value, 1e-9);
        Assert.AreNotEqual(yMinBefore, yAxis.MinLimit!.Value);
    }

    [TestMethod]
    public void CompositeXFlag_StillEnablesBothPanAndZoomOnX()
    {
        // Backward-compat guarantee: ZoomAndPanMode.X must keep pan+zoom semantics.
        var (_, xAxis, yAxis, core) = CreatePinnedChart();

        var xRangeBefore = xAxis.MaxLimit!.Value - xAxis.MinLimit!.Value;
        var yRangeBefore = yAxis.MaxLimit!.Value - yAxis.MinLimit!.Value;
        var xMinBefore = xAxis.MinLimit!.Value;

        core.Zoom(ZoomAndPanMode.X | ZoomAndPanMode.NoFit, new LvcPoint(200, 200), ZoomDirection.ZoomIn);
        core.Pan(ZoomAndPanMode.X | ZoomAndPanMode.NoFit, new LvcPoint(20, 0));

        Assert.IsTrue(xAxis.MaxLimit!.Value - xAxis.MinLimit!.Value < xRangeBefore);
        Assert.AreNotEqual(xMinBefore, xAxis.MinLimit!.Value);
        Assert.AreEqual(yRangeBefore, yAxis.MaxLimit!.Value - yAxis.MinLimit!.Value, 1e-9);
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
