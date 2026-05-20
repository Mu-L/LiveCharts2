using System;
using System.Collections.ObjectModel;
using System.Linq;
using CoreTests.Helpers;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Motion;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests.SeriesTests;

// HeatSeries cell animation. Each WeightedPoint becomes a colored rectangle. The
// rectangle position/size is what this harness captures — color interpolation is a
// separate channel (not captured by BoundedDrawnGeometry X/Y/W/H) and is covered by
// snapshot tests at the final frame.
[TestClass]
public class HeatSeriesAnimationTests
{
    private const long AnimationMs = 1000;
    private const long StepMs = 100;
    private const float Tolerance = 0.5f;

    private static void TriggerFirstMeasure(CartesianChartEngine core)
    {
        CoreMotionCanvas.DebugElapsedMilliseconds = 0;
        core.IsLoaded = true;
        core._isFirstDraw = true;
        core.Measure();
    }

    [TestMethod]
    public void HeatSeries_FirstDraw_CellsAppearAtMappedGridPositions()
    {
        var series = new HeatSeries<WeightedPoint>
        {
            Values =
            [
                new WeightedPoint(0, 0, 1),
                new WeightedPoint(1, 0, 5),
                new WeightedPoint(0, 1, 3),
                new WeightedPoint(1, 1, 9),
            ],
        };
        var chart = new SKCartesianChart
        {
            Width = 400,
            Height = 400,
            AnimationsSpeed = TimeSpan.FromMilliseconds(AnimationMs),
            EasingFunction = EasingFunctions.Lineal,
            Series = [series],
            XAxes = [new Axis { MinLimit = -0.5, MaxLimit = 1.5 }],
            YAxes = [new Axis { MinLimit = -0.5, MaxLimit = 1.5 }],
        };

        var core = (CartesianChartEngine)chart.CoreChart;

        try
        {
            TriggerFirstMeasure(core);

            var traj = SeriesAnimationCapture.CaptureTrajectory(
                series.everFetched, startMs: 0, endMs: AnimationMs, stepMs: StepMs);

            Assert.AreEqual(4, traj[0].Length, "expected 4 heat cells");

            // Final-frame cells fill their grid squares (W, H > 0).
            var finalFrame = traj[traj.Count - 1];
            foreach (var f in finalFrame)
            {
                Assert.IsTrue(f.Width > 0, "cell has non-zero width at final frame");
                Assert.IsTrue(f.Height > 0, "cell has non-zero height at final frame");
            }

            SeriesAnimationCapture.AssertTrajectoryMatches(traj, "HeatSeries_FirstDraw");
        }
        finally
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = -1;
        }
    }
}
