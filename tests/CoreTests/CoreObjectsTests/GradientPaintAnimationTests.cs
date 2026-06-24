using System;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Motion;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace CoreTests.CoreObjectsTests;

// The gradient paints animate their stops, points, radius and color positions in place via
// motion properties. These tests drive the motion clock by hand and read the (public) animated
// properties back to assert interpolation, completion, and the mismatched-length snap policy.
[TestClass]
public class GradientPaintAnimationTests
{
    [TestMethod]
    public void GradientStops_InterpolatePerChannelAndComplete()
    {
        var from = new[] { new SKColor(0, 0, 0, 255), new SKColor(0, 0, 0, 255) };
        var to = new[] { new SKColor(200, 100, 50, 255), new SKColor(100, 200, 150, 255) };
        var duration = TimeSpan.FromSeconds(1);

        var paint = new LinearGradientPaint(from);
        var animatable = (Animatable)paint;

        paint.SetTransition(
            new Animation(EasingFunctions.Lineal, duration), LinearGradientPaint.GradientStopsProperty);

        DrawFrame(0);
        paint.GradientStops = from;
        paint.CompleteTransition(LinearGradientPaint.GradientStopsProperty);

        paint.GradientStops = to;

        try
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = 500;
            animatable.IsValid = true;
            var mid = paint.GradientStops;

            for (var i = 0; i < to.Length; i++)
            {
                AssertChannel(from[i].Red, to[i].Red, 0.5f, mid[i].Red);
                AssertChannel(from[i].Green, to[i].Green, 0.5f, mid[i].Green);
                AssertChannel(from[i].Blue, to[i].Blue, 0.5f, mid[i].Blue);
            }
            Assert.IsFalse(animatable.IsValid, "paint must still be animating mid-flight.");

            DrawFrame(2000);
            CollectionAssert.AreEqual(to, paint.GradientStops);

            var motion = paint.GetPropertyDefinition(nameof(LinearGradientPaint.GradientStops))!.GetMotion(paint);
            Assert.IsTrue(motion.IsCompleted, "the gradient-stops transition must complete.");
        }
        finally
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = -1;
        }

        void DrawFrame(long time)
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = time;
            animatable.IsValid = true;
            _ = paint.GradientStops;
        }
    }

    [TestMethod]
    public void GradientStops_MismatchedLengthSnapsToTarget()
    {
        var from = new[] { new SKColor(0, 0, 0, 255), new SKColor(0, 0, 0, 255) };
        var to = new[] { SKColors.Red, SKColors.Green, SKColors.Blue };
        var duration = TimeSpan.FromSeconds(1);

        var paint = new LinearGradientPaint(from);
        var animatable = (Animatable)paint;

        paint.SetTransition(
            new Animation(EasingFunctions.Lineal, duration), LinearGradientPaint.GradientStopsProperty);

        CoreMotionCanvas.DebugElapsedMilliseconds = 0;
        animatable.IsValid = true;
        paint.GradientStops = from;
        paint.CompleteTransition(LinearGradientPaint.GradientStopsProperty);

        // A change to a different-length array cannot interpolate: it snaps to the target value
        // immediately rather than throwing or blending.
        paint.GradientStops = to;

        try
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = 500; // mid "transition"
            animatable.IsValid = true;
            CollectionAssert.AreEqual(to, paint.GradientStops);
        }
        finally
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = -1;
        }
    }

    [TestMethod]
    public void StartPoint_Interpolates()
    {
        var from = new SKPoint(0, 0);
        var to = new SKPoint(1, 1);
        var duration = TimeSpan.FromSeconds(1);

        var paint = new LinearGradientPaint([SKColors.Red, SKColors.Blue]);
        var animatable = (Animatable)paint;

        paint.SetTransition(
            new Animation(EasingFunctions.Lineal, duration), LinearGradientPaint.StartPointProperty);

        CoreMotionCanvas.DebugElapsedMilliseconds = 0;
        animatable.IsValid = true;
        paint.StartPoint = from;
        paint.CompleteTransition(LinearGradientPaint.StartPointProperty);

        paint.StartPoint = to;

        try
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = 500;
            animatable.IsValid = true;
            var mid = paint.StartPoint;
            Assert.AreEqual(0.5f, mid.X, 0.001f);
            Assert.AreEqual(0.5f, mid.Y, 0.001f);
        }
        finally
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = -1;
        }
    }

    [TestMethod]
    public void ColorPos_InterpolatesThenNullSnaps()
    {
        var from = new[] { 0f, 1f };
        var to = new[] { 0.2f, 0.8f };
        var duration = TimeSpan.FromSeconds(1);

        var paint = new LinearGradientPaint([SKColors.Red, SKColors.Blue], LinearGradientPaint.DefaultStartPoint, LinearGradientPaint.DefaultEndPoint, from);
        var animatable = (Animatable)paint;

        paint.SetTransition(
            new Animation(EasingFunctions.Lineal, duration), LinearGradientPaint.ColorPosProperty);

        CoreMotionCanvas.DebugElapsedMilliseconds = 0;
        animatable.IsValid = true;
        paint.ColorPos = from;
        paint.CompleteTransition(LinearGradientPaint.ColorPosProperty);

        paint.ColorPos = to;

        try
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = 500;
            animatable.IsValid = true;
            var mid = paint.ColorPos!;
            Assert.AreEqual(0.1f, mid[0], 0.001f);
            Assert.AreEqual(0.9f, mid[1], 0.001f);

            // A transition to null cannot interpolate: it snaps.
            paint.ColorPos = null;
            CoreMotionCanvas.DebugElapsedMilliseconds = 600;
            animatable.IsValid = true;
            Assert.IsNull(paint.ColorPos);
        }
        finally
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = -1;
        }
    }

    [TestMethod]
    public void RadialGradient_CenterInterpolates()
    {
        var from = new SKPoint(0f, 0f);
        var to = new SKPoint(1f, 1f);
        var duration = TimeSpan.FromSeconds(1);

        var paint = new RadialGradientPaint([SKColors.Red, SKColors.Blue], from);
        var animatable = (Animatable)paint;

        paint.SetTransition(
            new Animation(EasingFunctions.Lineal, duration), RadialGradientPaint.CenterProperty);

        CoreMotionCanvas.DebugElapsedMilliseconds = 0;
        animatable.IsValid = true;
        paint.Center = from;
        paint.CompleteTransition(RadialGradientPaint.CenterProperty);

        paint.Center = to;

        try
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = 500;
            animatable.IsValid = true;
            var mid = paint.Center;
            Assert.AreEqual(0.5f, mid.X, 0.001f);
            Assert.AreEqual(0.5f, mid.Y, 0.001f);
        }
        finally
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = -1;
        }
    }

    private static void AssertChannel(byte from, byte to, float progress, byte actual)
    {
        var expected = (byte)(from + progress * (to - from));
        Assert.IsTrue(
            Math.Abs(actual - expected) <= 1,
            $"channel expected ~{expected} but was {actual} at progress {progress}.");
    }
}
