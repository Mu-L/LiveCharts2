using System.Collections.Generic;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Providers;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace CoreTests.ChartTests;

// Guards the ISeriesRenderOverride provider seam: a provider's override must be
// consulted on all three data paths (render, bounds, hit-test) and on removal, and
// the chart must fall back to the series itself when the override declines.
[TestClass]
public class SeriesRenderOverrideTests
{
    private sealed class RecordingOverride : ISeriesRenderOverride
    {
        public bool RenderConsulted, BoundsConsulted, HitTestConsulted, Removed;

        // Test knobs for the engage-cleanup behaviour.
        public bool Engage;                       // TryRender returns this
        public bool Reuse;                        // ReusesSeriesPaints returns this
        public bool ReusesSeriesPaints => Reuse;

        // Record then take over / decline per Engage. When declining the chart falls back to the
        // series, so it renders exactly like OSS while we assert the seam was wired.
        public bool TryRender(ISeries series, Chart chart) { RenderConsulted = true; return Engage; }

        public bool TryGetBounds(
            ISeries series, Chart chart, ICartesianAxis secondaryAxis, ICartesianAxis primaryAxis,
            out SeriesBounds bounds)
        {
            BoundsConsulted = true;
            bounds = default;
            return false;
        }

        public IEnumerable<ChartPoint>? TryFindHitPoints(
            ISeries series, Chart chart, LvcPoint pointerPosition, FindingStrategy strategy, FindPointFor findPointFor)
        {
            HitTestConsulted = true;
            return null;
        }

        public void OnRemoved(IChartView view, ISeries series) => Removed = true;
    }

    private sealed class FakeEngine(RecordingOverride over) : SkiaSharpProvider
    {
        // Passthrough SkiaSharp engine that only adds the override (for any series),
        // so configuring it globally for the test doesn't change anything else.
        public override ISeriesRenderOverride? GetRenderOverride(ISeries series) => over;
    }

    [TestMethod]
    public void Override_IsConsultedOnAllPaths_AndChartFallsBack()
    {
        var over = new RecordingOverride();
        try
        {
            LiveCharts.Configure(s => s.HasProvider(new FakeEngine(over)));

            var series = new LineSeries<double> { Values = [1, 2, 3, 4], GeometrySize = 0 };
            var chart = new SKCartesianChart
            {
                Width = 600,
                Height = 400,
                Series = [series],
                XAxes = [new Axis()],
                YAxes = [new Axis()]
            };

            // Measure: rendering (AddVisual) + bounds (Measure) consult the override.
            var image = chart.GetImage();
            Assert.IsNotNull(image, "chart must still render via the fallback when the override declines");
            Assert.IsTrue(over.RenderConsulted, "TryRender must be consulted during measure");
            Assert.IsTrue(over.BoundsConsulted, "TryGetBounds must be consulted during measure");

            // Hit-test path consults the override (enumerate — GetPointsAt is lazy).
            _ = chart.GetPointsAt(new LvcPointD(300, 200), FindingStrategy.CompareAll).ToArray();
            Assert.IsTrue(over.HitTestConsulted, "TryFindHitPoints must be consulted on hit-test");
        }
        finally
        {
            LiveCharts.Configure(s => s.AddSkiaSharp());
        }
    }

    // When an override ENGAGES it bypasses the series' per-point Invalidate, so the series' last
    // drawn visuals would linger frozen on the canvas. The chart drops them — UNLESS the override
    // declares it REUSES the series' own paints (then it manages them itself and the chart keeps
    // them). Drives two measures on a loaded chart so there ARE prior OSS visuals to drop.
    [TestMethod]
    public void EngagingOverride_DropsTheSeriesOwnVisuals_UnlessItReusesThePaints()
    {
        var over = new RecordingOverride();
        try
        {
            LiveCharts.Configure(s => s.HasProvider(new FakeEngine(over)));

            static SKCartesianChart NewChart() => new()
            {
                Width = 600,
                Height = 400,
                Series = [new LineSeries<double> { Values = [1, 2, 3, 4], GeometrySize = 0 }],
                XAxes = [new Axis()],
                YAxes = [new Axis()],
            };

            int Measure(SKCartesianChart chart, bool engage, bool reuse, bool firstDraw)
            {
                over.Engage = engage;
                over.Reuse = reuse;
                var core = chart.CoreChart;
                core.IsLoaded = true;
                core._isFirstDraw = firstDraw;
                core.Measure();
                using var surface = SKSurface.Create(new SKImageInfo(10, 10));
                chart.CoreCanvas.DrawFrame(new SkiaSharpDrawingContext(chart.CoreCanvas, surface.Canvas, SKColor.Empty));
                return chart.CoreCanvas.CountPaintTasks();
            }

            // Override engages, does NOT reuse → the chart drops the series' frozen visuals.
            var chartA = NewChart();
            var ossTasks = Measure(chartA, engage: false, reuse: false, firstDraw: true); // OSS draws its paints
            var droppedTasks = Measure(chartA, engage: true, reuse: false, firstDraw: false);
            Assert.IsTrue(droppedTasks < ossTasks,
                $"a non-reusing engage must drop the series' own visuals (oss={ossTasks}, engaged={droppedTasks})");

            // Fresh chart: override engages AND reuses → the chart keeps the series' paints (the
            // override is responsible for them), so they are NOT dropped.
            var chartB = NewChart();
            var ossTasks2 = Measure(chartB, engage: false, reuse: false, firstDraw: true);
            var reusedTasks = Measure(chartB, engage: true, reuse: true, firstDraw: false);
            Assert.AreEqual(ossTasks2, reusedTasks,
                $"a reusing engage must keep the series' paints (oss={ossTasks2}, engaged={reusedTasks})");
        }
        finally
        {
            LiveCharts.Configure(s => s.AddSkiaSharp());
        }
    }
}
