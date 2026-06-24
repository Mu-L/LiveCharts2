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

// A paint assigned directly to a geometry (an override on IDrawnElement.Fill/Stroke, not a registered
// paint task) advances its own motion clock while drawing, but the frame loop only folds geometries and
// registered tasks into its "needs another frame?" decision. Without help, an animating per-element
// paint would freeze after the first frame — it only progressed while some *other* animation (or mouse
// movement) kept the canvas dirty. The drawing context folds the per-element paint's validity into the
// element so the override animates on its own, then lets the canvas settle once the transition completes.
[TestClass]
public class PerElementPaintAnimationTests
{
    [TestMethod]
    public void AnimatingPerElementFill_KeepsCanvasInvalid_ThenSettles()
    {
        CoreMotionCanvas.DebugElapsedMilliseconds = 0;

        try
        {
            using var surface = SKSurface.Create(new SKImageInfo(100, 100));
            var canvas = new CoreMotionCanvas();

            // a per-element fill with a 1s color transition red -> blue
            var fill = new SolidColorPaint(SKColors.Red);
            fill.SetTransition(
                new Animation(EasingFunctions.Lineal, TimeSpan.FromSeconds(1)),
                SolidColorPaint.ColorProperty);

            // the geometry carries the paint as its own Fill (not a registered series paint task);
            // its own geometry properties are static, so only the fill animates.
            var rectangle = new RectangleGeometry { X = 0, Y = 0, Width = 10, Height = 10, Fill = fill };
            _ = canvas.AddGeometry(rectangle);

            // start the transition at t = 0
            fill.Color = SKColors.Blue;

            // mid-animation: the canvas must report it needs another frame even though nothing else
            // is animating. Before the per-element fold, this latched valid and the color froze.
            CoreMotionCanvas.DebugElapsedMilliseconds = 500;
            DrawFrame(canvas, surface);
            Assert.IsFalse(
                canvas.IsValid,
                "an animating per-element Fill must keep the canvas wanting frames on its own.");

            // past the end: the transition completes, so the canvas must settle (no perpetual redraw).
            CoreMotionCanvas.DebugElapsedMilliseconds = 2000;
            DrawFrame(canvas, surface);
            Assert.IsTrue(
                canvas.IsValid,
                "once the per-element Fill transition completes, the canvas must settle.");
        }
        finally
        {
            CoreMotionCanvas.DebugElapsedMilliseconds = -1;
        }
    }

    private static void DrawFrame(CoreMotionCanvas canvas, SKSurface surface) =>
        canvas.DrawFrame(new SkiaSharpDrawingContext(canvas, surface.Canvas, SKColor.Empty));
}
