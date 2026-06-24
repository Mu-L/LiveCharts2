using System;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Motion;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace CoreTests.CoreObjectsTests;

// A paint reference is not interpolated (animation lives inside the paint). A geometry's Fill is a
// visual-state target (a registered PropertyDefinition) but NOT a motion property: assigning a new
// paint snaps to it, and the definition exposes no motion. (Paint.Transitionate, the old in-place
// two-paint blend, was removed earlier; the by-reference motion backing was removed in this change.)
[TestClass]
public class PaintReferenceMotionTests
{
    [TestMethod]
    public void Fill_SnapsToNewPaint_AndIsNotBackedByAMotion()
    {
        var geometry = new RectangleGeometry();
        var paintA = new SolidColorPaint(SKColors.Red);
        var paintB = new SolidColorPaint(SKColors.Blue);

        geometry.Fill = paintA;
        Assert.AreSame(paintA, geometry.Fill);

        // assigning a new paint snaps to it — references are never blended.
        geometry.Fill = paintB;
        Assert.AreSame(paintB, geometry.Fill);

        // Fill is registered (states can target it) but motionless: it must not animate, so the
        // definition has no motion — which also means no FromValue pins the previous paint instance.
        var definition = geometry.GetPropertyDefinition(nameof(DrawnGeometry.Fill))!;
        Assert.IsNull(
            definition.GetMotion(geometry),
            "a paint reference must be a state property, not a motion property.");
    }

    // ByReferenceMotionProperty is no longer used to back the geometry paint references, but it remains
    // public API. Pin its contract directly: it snaps to the assigned value and reports completion even
    // with an animation configured (a non-interpolated transition is trivially done — the early-return
    // path must still run OnCompleted, the base regression behind the original fix).
    [TestMethod]
    public void ByReferenceMotionProperty_Snaps_AndReportsCompleted()
    {
        var host = new RectangleGeometry();
        var property = new ByReferenceMotionProperty<object?>
        {
            Animation = new Animation(EasingFunctions.Lineal, TimeSpan.FromSeconds(1))
        };

        CoreMotionCanvas.DebugElapsedMilliseconds = 0;
        var target = new object();
        property.SetMovement(target, host);

        try
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = 500;
            Assert.AreSame(target, property.GetMovement(host), "a by-reference value snaps to the target.");
            Assert.IsTrue(property.IsCompleted, "a non-interpolated transition must report completion.");
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
