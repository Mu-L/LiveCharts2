using System;
using System.Linq;
using CoreTests.Helpers;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Motion;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests.SeriesTests;

// Per-frame animation contract tests for bar-family series. Drive coreChart.Measure() with
// CoreMotionCanvas.DebugElapsedMilliseconds; assert that each visual interpolates linearly
// between its from/to values over the animation duration.
//
// Charts use EasingFunctions.Lineal and fixed axis limits so the math is deterministic:
//   - SKCartesianChart constructor sets EasingFunction = null (animations off by default
//     in test-mode) — we explicitly set Lineal so transitions interpolate
//   - Fixed MinLimit/MaxLimit prevents axis bounds from shifting between measures, which
//     would invalidate intermediate trajectory frames
//
// These tests are the contract that downstream bar-series refactors must preserve.
[TestClass]
public class BarSeriesAnimationTests
{
    private const long AnimationMs = 1000;
    private const long StepMs = 100; // 11 frames: 0%, 10%, 20%, ..., 100%
    private const float Tolerance = 0.5f;

    private static void TriggerFirstMeasure(CartesianChartEngine core)
    {
        CoreMotionCanvas.DebugElapsedMilliseconds = 0;
        core.IsLoaded = true;
        core._isFirstDraw = true;
        core.Measure();
    }

    private static void AssertLinearInterpolation(
        System.Collections.Generic.List<SeriesAnimationCapture.Frame[]> traj,
        System.Func<SeriesAnimationCapture.Frame, float> dimension,
        string label)
    {
        var firstFrame = traj[0];
        var finalFrame = traj[^1];
        var trajectoryStart = firstFrame[0].TimeMs;

        for (var frameIdx = 1; frameIdx < traj.Count - 1; frameIdx++)
        {
            var frame = traj[frameIdx];
            var progress = (float)(frame[0].TimeMs - trajectoryStart) / AnimationMs;

            for (var pointIdx = 0; pointIdx < frame.Length; pointIdx++)
            {
                var startValue = dimension(firstFrame[pointIdx]);
                var endValue = dimension(finalFrame[pointIdx]);
                var expected = startValue + progress * (endValue - startValue);
                Assert.AreEqual(expected, dimension(frame[pointIdx]), Tolerance,
                    $"{label}: at t={frame[pointIdx].TimeMs} (p={progress:F2}), point {pointIdx} " +
                    $"expected≈{expected}, got {dimension(frame[pointIdx])}");
            }
        }
    }

    [TestMethod]
    public void ColumnSeries_FirstDraw_BarsGrowFromPivotToFinalHeight()
    {
        var series = new ColumnSeries<double> { Values = [10d, 20d, 30d] };
        var chart = new SKCartesianChart
        {
            Width = 400,
            Height = 400,
            AnimationsSpeed = TimeSpan.FromMilliseconds(AnimationMs),
            EasingFunction = EasingFunctions.Lineal,
            Series = [series],
            YAxes = [new Axis { MinLimit = 0, MaxLimit = 40 }],
        };

        var core = (CartesianChartEngine)chart.CoreChart;

        try
        {
            TriggerFirstMeasure(core);

            var traj = SeriesAnimationCapture.CaptureTrajectory(
                series.everFetched, startMs: 0, endMs: AnimationMs, stepMs: StepMs);

            Assert.AreEqual(11, traj.Count);
            Assert.AreEqual(3, traj[0].Length);

            // bars enter at Height=0 (visual just created at pivot)
            foreach (var f in traj[0])
                Assert.AreEqual(0f, f.Height, Tolerance, "first-draw entry height");

            // final-frame heights are non-zero and monotonically increase with Values
            var finalFrame = traj[^1];
            Assert.IsTrue(finalFrame[0].Height > 0);
            Assert.IsTrue(finalFrame[0].Height < finalFrame[1].Height);
            Assert.IsTrue(finalFrame[1].Height < finalFrame[2].Height);

            AssertLinearInterpolation(traj, f => f.Height, "column-first-draw-height");
            AssertLinearInterpolation(traj, f => f.Y, "column-first-draw-Y");
        }
        finally
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = -1;
        }
    }
}
