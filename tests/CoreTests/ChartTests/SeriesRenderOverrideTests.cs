using System.Collections.Generic;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Providers;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

        // Record then DECLINE on every path → the chart falls back to the series,
        // so it renders exactly like OSS while we assert the seam was wired.
        public bool TryRender(ISeries series, Chart chart) { RenderConsulted = true; return false; }

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
}
