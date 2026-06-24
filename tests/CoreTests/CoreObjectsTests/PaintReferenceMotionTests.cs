using System;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Motion;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace CoreTests.CoreObjectsTests;

// A paint reference is not interpolated (animation lives inside the paint). Assigning a new paint
// to a geometry's Fill snaps to it, and the motion must report completion even with an animation
// configured — a non-interpolated transition is trivially done.
[TestClass]
public class PaintReferenceMotionTests
{
    [TestMethod]
    public void Fill_SnapsToNewPaintAndReportsCompleted()
    {
        var geometry = new RectangleGeometry();
        var animatable = (Animatable)geometry;
        var paintA = new SolidColorPaint(SKColors.Red);
        var paintB = new SolidColorPaint(SKColors.Blue);

        geometry.SetTransition(
            new Animation(EasingFunctions.Lineal, TimeSpan.FromSeconds(1)), DrawnGeometry.FillProperty);

        CoreMotionCanvas.DebugElapsedMilliseconds = 0;
        animatable.IsValid = true;
        geometry.Fill = paintA;
        geometry.CompleteTransition(DrawnGeometry.FillProperty);

        // Assign a new paint while a transition + animation are configured.
        CoreMotionCanvas.DebugElapsedMilliseconds = 500;
        animatable.IsValid = true;
        geometry.Fill = paintB;

        try
        {
            // The reference snaps to the new paint (no blending between instances).
            Assert.AreSame(paintB, geometry.Fill);

            // Regression: before the base fix the motion stayed IsCompleted == false forever because
            // GetMovement returns early when it cannot interpolate, never running the completion path.
            var motion = geometry.GetPropertyDefinition(nameof(DrawnGeometry.Fill))!.GetMotion(geometry);
            Assert.IsTrue(motion.IsCompleted, "a non-interpolated paint transition must report completion.");
        }
        finally
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = -1;
        }
    }

    // Replacing a geometry's fill must NOT mutate the previous paint instance. Regression guard for
    // the in-place blend this PR removes: the old Paint.Transitionate lerped the previous paint's
    // color in place, corrupting visual-state paints shared across points (e.g. ConditionalDraw's
    // shared red paint stopped being red after the first clear). With references snapping, the
    // previous paint is left untouched.
    [TestMethod]
    public void ReplacingFill_DoesNotMutateThePreviousPaint()
    {
        var red = new SKColor(255, 0, 0);
        var shared = new SolidColorPaint(red); // stands in for a state paint shared across points
        var geometry = new RectangleGeometry();
        var animatable = (Animatable)geometry;

        // Animate the geometry the way a series does, so any transition machinery is exercised.
        geometry.SetTransition(new Animation(EasingFunctions.Lineal, TimeSpan.FromSeconds(1)));

        try
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = 0;
            animatable.IsValid = true;
            geometry.Fill = shared;
            Assert.AreSame(shared, geometry.Fill);

            // Replace the fill (as clearing/changing a state does) and drive a full second of frames.
            geometry.Fill = new SolidColorPaint(new SKColor(0, 0, 255));
            for (long t = 0; t <= 1000; t += 100)
            {
                CoreMotionCanvas.DebugElapsedMilliseconds = t;
                animatable.IsValid = true;
                _ = geometry.Fill;
            }

            Assert.AreEqual(red, shared.Color,
                "replacing a geometry's fill must not mutate the previous paint instance.");
        }
        finally
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = -1;
        }
    }
}
