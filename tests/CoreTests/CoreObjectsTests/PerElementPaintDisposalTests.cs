using System;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Motion;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace CoreTests.CoreObjectsTests;

// A paint assigned directly to a geometry (an override on IDrawnElement.Fill/Stroke/Paint, not a
// registered paint task) is never visited by the frame loop's RemoveOnCompleted handling, which only
// acts on tasks and geometries. So a per-element override — e.g. one created per hover — was never
// detached or disposed, orphaning its native SKPaint until finalization. The drawing context now
// honors RemoveOnCompleted on per-element paints: once the paint's transition completes it is detached
// from the element and its task disposed.
[TestClass]
public class PerElementPaintDisposalTests
{
    private sealed class TrackingPaint(SKColor color) : SolidColorPaint(color)
    {
        public int DisposeCount { get; private set; }

        internal override void DisposeTask()
        {
            DisposeCount++;
            base.DisposeTask();
        }
    }

    [TestMethod]
    public void PerElementFill_WithRemoveOnCompleted_IsDetachedAndDisposed_WhenTransitionCompletes()
    {
        CoreMotionCanvas.DebugElapsedMilliseconds = 0;

        try
        {
            using var surface = SKSurface.Create(new SKImageInfo(100, 100));
            var canvas = new CoreMotionCanvas();

            var fill = new TrackingPaint(SKColors.Red);
            fill.SetTransition(
                new Animation(EasingFunctions.Lineal, TimeSpan.FromSeconds(1)),
                SolidColorPaint.ColorProperty);
            fill.RemoveOnCompleted = true;

            var rectangle = new RectangleGeometry { X = 0, Y = 0, Width = 10, Height = 10, Fill = fill };
            _ = canvas.AddGeometry(rectangle);

            // start the transition at t = 0
            fill.Color = SKColors.Blue;

            // mid-animation: the override is still attached and must not be disposed yet.
            CoreMotionCanvas.DebugElapsedMilliseconds = 500;
            DrawFrame(canvas, surface);
            Assert.AreSame(
                fill, rectangle.Fill,
                "a RemoveOnCompleted per-element paint must stay attached while it is still animating.");
            Assert.AreEqual(0, fill.DisposeCount, "...and must not be disposed mid-transition.");

            // past the end: the transition completes, so the override must be detached and disposed.
            CoreMotionCanvas.DebugElapsedMilliseconds = 2000;
            DrawFrame(canvas, surface);
            Assert.IsNull(
                rectangle.Fill,
                "a completed RemoveOnCompleted per-element paint must be detached from the element.");
            Assert.AreEqual(
                1, fill.DisposeCount,
                "...and its task disposed exactly once (no orphaned SKPaint).");
        }
        finally
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = -1;
        }
    }

    [TestMethod]
    public void PerElementFill_WithoutRemoveOnCompleted_StaysAttached_AfterTransitionCompletes()
    {
        CoreMotionCanvas.DebugElapsedMilliseconds = 0;

        try
        {
            using var surface = SKSurface.Create(new SKImageInfo(100, 100));
            var canvas = new CoreMotionCanvas();

            var fill = new TrackingPaint(SKColors.Red);
            fill.SetTransition(
                new Animation(EasingFunctions.Lineal, TimeSpan.FromSeconds(1)),
                SolidColorPaint.ColorProperty);

            var rectangle = new RectangleGeometry { X = 0, Y = 0, Width = 10, Height = 10, Fill = fill };
            _ = canvas.AddGeometry(rectangle);

            fill.Color = SKColors.Blue;

            CoreMotionCanvas.DebugElapsedMilliseconds = 2000;
            DrawFrame(canvas, surface);

            // default behavior is unchanged: without RemoveOnCompleted the override is kept.
            Assert.AreSame(fill, rectangle.Fill);
            Assert.AreEqual(0, fill.DisposeCount);
        }
        finally
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = -1;
        }
    }

    private static void DrawFrame(CoreMotionCanvas canvas, SKSurface surface) =>
        canvas.DrawFrame(new SkiaSharpDrawingContext(canvas, surface.Canvas, SKColor.Empty));
}
