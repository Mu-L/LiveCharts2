using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Motion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    public readonly struct Frame
    {
        public Frame(long timeMs, float x, float y, float width, float height)
        {
            TimeMs = timeMs;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public long TimeMs { get; }
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
    }

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

    // Snapshot-style assertion: compares the captured trajectory against a JSON baseline
    // committed at tests/CoreTests/AnimationBaselines/{baselineName}.json. Mirrors the
    // workflow in tests/SnapshotTests/Extensions.cs#AssertSnapshotMatches:
    //   - First run with no baseline: writes the new trajectory to AnimationBaselinesNew/
    //     and fails with a "review and commit" message
    //   - Subsequent runs: deserializes the baseline and compares frame-by-frame within
    //     `tolerance` pixels per dimension. On mismatch writes both [EXPECTED] and [RESULT]
    //     to AnimationBaselinesDiff/ for inspection
    //
    // Re-baseline workflow: delete tests/CoreTests/AnimationBaselines/{baselineName}.json,
    // re-run the test, move the regenerated file from AnimationBaselinesNew/ back into place.
    public static void AssertTrajectoryMatches(
        List<Frame[]> trajectory,
        string baselineName,
        float tolerance = 0.5f)
    {
        var baseDir = AppContext.BaseDirectory;
        var baselineDir = Path.Combine(baseDir, "AnimationBaselines");
        var newDir = Path.Combine(baseDir, "AnimationBaselinesNew");
        var diffDir = Path.Combine(baseDir, "AnimationBaselinesDiff");
        var baselinePath = Path.Combine(baselineDir, $"{baselineName}.json");
        var newPath = Path.Combine(newDir, $"{baselineName}.json");

        _ = Directory.CreateDirectory(newDir);

        var serialized = Serialize(trajectory);
        File.WriteAllText(newPath, serialized);

        if (!File.Exists(baselinePath))
        {
            Assert.Fail(
                $"Animation baseline not found for '{baselineName}'. " +
                $"A new trajectory was written to '{newPath}'. Review it and commit " +
                $"to tests/CoreTests/AnimationBaselines/{baselineName}.json.");
            return;
        }

        var expected = Deserialize(File.ReadAllText(baselinePath));

        var mismatch = Compare(expected, trajectory, tolerance);
        if (mismatch is null) return;

        _ = Directory.CreateDirectory(diffDir);
        File.Copy(baselinePath, Path.Combine(diffDir, $"{baselineName}[EXPECTED].json"), overwrite: true);
        File.Copy(newPath, Path.Combine(diffDir, $"{baselineName}[RESULT].json"), overwrite: true);

        Assert.Fail(
            $"Trajectory '{baselineName}' diverges from baseline: {mismatch}. " +
            $"See AnimationBaselinesDiff/{baselineName}[EXPECTED|RESULT].json.");
    }

    private static string Serialize(List<Frame[]> trajectory)
    {
        // Compact one-line-per-frame layout — easy to diff in PRs.
        var sb = new StringBuilder();
        _ = sb.Append('[');
        _ = sb.Append('\n');
        for (var i = 0; i < trajectory.Count; i++)
        {
            _ = sb.Append("  [");
            var frame = trajectory[i];
            for (var j = 0; j < frame.Length; j++)
            {
                if (j > 0) _ = sb.Append(',');
                var f = frame[j];
                _ = sb.Append(
                    "{\"t\":").Append(f.TimeMs)
                    .Append(",\"x\":").Append(F(f.X))
                    .Append(",\"y\":").Append(F(f.Y))
                    .Append(",\"w\":").Append(F(f.Width))
                    .Append(",\"h\":").Append(F(f.Height))
                    .Append('}');
            }
            _ = sb.Append(']');
            if (i < trajectory.Count - 1) _ = sb.Append(',');
            _ = sb.Append('\n');
        }
        _ = sb.Append(']');
        _ = sb.Append('\n');
        return sb.ToString();

        static string F(float v) => v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static List<Frame[]> Deserialize(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var trajectory = new List<Frame[]>();
        foreach (var frameElement in doc.RootElement.EnumerateArray())
        {
            var frame = new List<Frame>();
            foreach (var pointElement in frameElement.EnumerateArray())
            {
                frame.Add(new Frame(
                    pointElement.GetProperty("t").GetInt64(),
                    pointElement.GetProperty("x").GetSingle(),
                    pointElement.GetProperty("y").GetSingle(),
                    pointElement.GetProperty("w").GetSingle(),
                    pointElement.GetProperty("h").GetSingle()));
            }
            trajectory.Add([.. frame]);
        }
        return trajectory;
    }

    private static string? Compare(List<Frame[]> expected, List<Frame[]> actual, float tolerance)
    {
        if (expected.Count != actual.Count)
            return $"frame count {expected.Count} expected, got {actual.Count}";

        for (var i = 0; i < expected.Count; i++)
        {
            var e = expected[i];
            var a = actual[i];
            if (e.Length != a.Length)
                return $"frame {i}: point count {e.Length} expected, got {a.Length}";

            for (var j = 0; j < e.Length; j++)
            {
                if (e[j].TimeMs != a[j].TimeMs)
                    return $"frame {i} point {j}: TimeMs {e[j].TimeMs} expected, got {a[j].TimeMs}";

                if (Math.Abs(e[j].X - a[j].X) > tolerance)
                    return $"frame {i} (t={a[j].TimeMs}) point {j}: X expected {e[j].X}, got {a[j].X}";
                if (Math.Abs(e[j].Y - a[j].Y) > tolerance)
                    return $"frame {i} (t={a[j].TimeMs}) point {j}: Y expected {e[j].Y}, got {a[j].Y}";
                if (Math.Abs(e[j].Width - a[j].Width) > tolerance)
                    return $"frame {i} (t={a[j].TimeMs}) point {j}: Width expected {e[j].Width}, got {a[j].Width}";
                if (Math.Abs(e[j].Height - a[j].Height) > tolerance)
                    return $"frame {i} (t={a[j].TimeMs}) point {j}: Height expected {e[j].Height}, got {a[j].Height}";
            }
        }
        return null;
    }
}
