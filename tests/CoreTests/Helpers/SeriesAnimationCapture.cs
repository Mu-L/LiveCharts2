using System.Collections.Generic;
using System.Linq;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Motion;

namespace CoreTests.Helpers;

// Drives an SK chart's CoreChart.Measure() in a controlled-time loop and captures every
// visible point's BoundedDrawnGeometry rect at each sample timestamp. Pattern modeled on
// CoreObjectsTests.TransitionsTesting (which exercises raw RectangleGeometry) and
// OtherTests.AnimationsSpeedPropagationTests.RuntimeAnimationsSpeedChange_Interpolates_AtNewSpeed
// (which exercises a single visual property across a single transition).
//
// Caller responsibility:
//   - Build a chart with EasingFunctions.Lineal so interpolation is predictable
//   - Fix axis MinLimit/MaxLimit so a re-measure doesn't shift visuals
//   - Wrap the call in try/finally and reset CoreMotionCanvas.DebugElapsedMilliseconds = -1
internal static class SeriesAnimationCapture
{
    public readonly record struct Frame(long TimeMs, float X, float Y, float Width, float Height);

    public static List<Frame[]> CaptureTrajectory(
        IEnumerable<ChartPoint> points,
        long startMs,
        long endMs,
        long stepMs)
    {
        var pointList = points.ToList();
        var trajectory = new List<Frame[]>();

        for (var t = startMs; t <= endMs; t += stepMs)
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = t;

            var frame = pointList
                .Where(p => p.Context.Visual is BoundedDrawnGeometry)
                .Select(p =>
                {
                    var v = (BoundedDrawnGeometry)p.Context.Visual!;
                    // Property getters interpolate to DebugElapsedMilliseconds when read.
                    return new Frame(t, v.X, v.Y, v.Width, v.Height);
                })
                .ToArray();

            trajectory.Add(frame);
        }

        return trajectory;
    }
}
