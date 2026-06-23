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
}
