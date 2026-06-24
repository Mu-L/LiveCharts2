using System;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Motion;
using LiveChartsCore.Painting;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace CoreTests.CoreObjectsTests;

// Paints animate by mutating their own state on the same instance (their interpolable
// properties are motion properties). These tests drive the motion clock by hand and assert
// that Color and StrokeThickness interpolate from the previous value toward the new one and
// then settle — i.e. the paint stops invalidating once the transition completes.
[TestClass]
public class SolidColorPaintAnimationTests
{
    [TestMethod]
    public void Color_InterpolatesBetweenValuesAndCompletes()
    {
        var from = new SKColor(0, 0, 0, 255);
        var to = new SKColor(200, 100, 50, 255);
        var duration = TimeSpan.FromSeconds(1);

        var paint = new SolidColorPaint();
        var animatable = (Animatable)paint;

        paint.SetTransition(
            new Animation(EasingFunctions.Lineal, duration), SolidColorPaint.ColorProperty);

        void DrawFrame(long time)
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = time;
            animatable.IsValid = true;
            _ = paint.Color; // reading the getter advances the transition with the current clock
        }

        // Baseline: snap the color to `from` so the next assignment animates from a known value.
        DrawFrame(0);
        paint.Color = from;
        paint.CompleteTransition(SolidColorPaint.ColorProperty);

        // Start the transition `from` -> `to` at t = 0.
        paint.Color = to;

        try
        {
            // Mid-flight samples: each channel is a linear blend, and the paint is still invalid
            // (the getter flags it so the canvas keeps drawing).
            foreach (var t in new long[] { 250, 500, 750 })
            {
                CoreMotionCanvas.DebugElapsedMilliseconds = t;
                animatable.IsValid = true;
                var c = paint.Color;
                var p = t / (float)duration.TotalMilliseconds;

                AssertChannel(from.Red, to.Red, p, c.Red);
                AssertChannel(from.Green, to.Green, p, c.Green);
                AssertChannel(from.Blue, to.Blue, p, c.Blue);
                Assert.AreEqual(255, c.Alpha);
                Assert.IsFalse(animatable.IsValid, $"paint must still be animating at t={t}.");
            }

            // Past the end the color equals the target and the transition is completed
            // (the paint no longer invalidates itself, so the canvas can stop drawing).
            DrawFrame(2000);
            Assert.AreEqual(to, paint.Color);

            var motion = paint.GetPropertyDefinition(nameof(SolidColorPaint.Color))!.GetMotion(paint);
            Assert.IsTrue(motion.IsCompleted, "the color transition must complete at the end.");
        }
        finally
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = -1;
        }
    }

    [TestMethod]
    public void StrokeThickness_InterpolatesBetweenValuesAndCompletes()
    {
        const float from = 2f;
        const float to = 10f;
        var duration = TimeSpan.FromSeconds(1);

        var paint = new SolidColorPaint();
        var animatable = (Animatable)paint;

        paint.SetTransition(
            new Animation(EasingFunctions.Lineal, duration), Paint.StrokeThicknessProperty);

        DrawFrame(0);
        paint.StrokeThickness = from;
        paint.CompleteTransition(Paint.StrokeThicknessProperty);

        paint.StrokeThickness = to;

        try
        {
            foreach (var t in new long[] { 250, 500, 750 })
            {
                CoreMotionCanvas.DebugElapsedMilliseconds = t;
                animatable.IsValid = true;
                var value = paint.StrokeThickness;
                var p = t / (float)duration.TotalMilliseconds;

                Assert.AreEqual(from + p * (to - from), value, 0.001f);
                Assert.IsFalse(animatable.IsValid, $"paint must still be animating at t={t}.");
            }

            DrawFrame(2000);
            Assert.AreEqual(to, paint.StrokeThickness, 0.001f);

            var motion = paint.GetPropertyDefinition(nameof(Paint.StrokeThickness))!.GetMotion(paint);
            Assert.IsTrue(motion.IsCompleted, "the stroke-thickness transition must complete at the end.");
        }
        finally
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = -1;
        }

        void DrawFrame(long time)
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = time;
            animatable.IsValid = true;
            _ = paint.StrokeThickness;
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
