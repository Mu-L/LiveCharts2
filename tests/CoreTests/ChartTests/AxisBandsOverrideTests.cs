using System;
using System.Collections.Generic;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Providers;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Painting;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace CoreTests.ChartTests;

// Guards the IAxisBandsOverride capability of the axis render-override seam: when the engine's
// override also implements it, the axis draws the bands it returns as rectangles in the draw
// margin (full size on the other dimension), keyed by start value, and animates them out when
// the override stops returning them. The engine still decides which axes are overridden.
[TestClass]
public class AxisBandsOverrideTests
{
    private sealed class RecordingBands : IAxisRenderOverride, IAxisBandsOverride
    {
        public bool Enabled = true;
        public bool Consulted;
        public IReadOnlyList<double>? SeenSeparators;
        public Paint Paint = new SolidColorPaint(SKColors.LightGray);

        public bool TryGroup(
            ICartesianAxis axis, Chart chart, double min, double max,
            out IEnumerable<double>? separators, out Func<double, string>? labeler)
        {
            separators = null;
            labeler = null;
            return false;
        }

        public BoundedDrawnGeometry CreateBandGeometry() => new RectangleGeometry();

        public bool TryGetBands(
            ICartesianAxis axis, Chart chart, double min, double max, IReadOnlyList<double> separators,
            out IEnumerable<AxisBand>? bands, out Paint? paint)
        {
            Consulted = true;
            SeenSeparators = separators;

            if (!Enabled)
            {
                bands = null;
                paint = null;
                return false;
            }

            bands = [new AxisBand(10, 20), new AxisBand(30, 40)];
            paint = Paint;
            return true;
        }
    }

    private sealed class FakeEngine(RecordingBands bands) : SkiaSharpProvider
    {
        public ICartesianAxis? Target;

        public override IAxisRenderOverride? GetAxisRenderOverride(ICartesianAxis axis) =>
            ReferenceEquals(axis, Target) ? bands : null;
    }

    private static SKCartesianChart NewChart(ICartesianAxis xAxis) => new()
    {
        Width = 600,
        Height = 400,
        Series = [new LineSeries<double> { Values = [0, 100], GeometrySize = 0 }],
        XAxes = [xAxis],
        YAxes = [new Axis()],
    };

    [TestMethod]
    public void Bands_AreDrawn_AsDrawMarginSizedRectangles()
    {
        var bands = new RecordingBands();
        try
        {
            var xAxis = new Axis { MinLimit = 0, MaxLimit = 100 };
            LiveCharts.Configure(s => s.HasProvider(new FakeEngine(bands) { Target = xAxis }));

            var chart = NewChart(xAxis);
            _ = CoreObjectsTests.ChangingPaintTasks.DrawChart(chart);

            Assert.IsTrue(bands.Consulted, "the bands override must be consulted on measure");
            Assert.IsTrue(
                bands.SeenSeparators is { Count: > 0 },
                "the override must receive the axis' separator values");

            var rects = bands.Paint.GetGeometries(chart.CoreCanvas).Cast<BoundedDrawnGeometry>().ToArray();
            Assert.AreEqual(2, rects.Length, "one rectangle per band");

            var drawMarginHeight = chart.CoreChart.DrawMarginSize.Height;
            foreach (var rect in rects)
            {
                Assert.IsTrue(rect.Width > 0, "an X-axis band has the band range as width");
                Assert.AreEqual(drawMarginHeight, rect.Height, 0.5, "an X-axis band spans the draw margin height");
            }

            // 10→20 and 30→40 over 0..100 are equally wide and not overlapping.
            Assert.AreEqual(rects[0].Width, rects[1].Width, 0.5, "equal ranges produce equal widths");
            Assert.AreNotEqual(rects[0].X, rects[1].X, "bands sit at their own positions");
        }
        finally
        {
            LiveCharts.Configure(s => s.AddSkiaSharp());
        }
    }

    [TestMethod]
    public void Bands_Rasterize()
    {
        // The bands paint comes from the override, not from an axis property setter, so its
        // PaintStyle is Undefined — which draws neither fill nor stroke. The axis must default
        // it to Fill, otherwise the bands exist as geometries but never reach a single pixel
        // (which is exactly how this was first caught).
        var bands = new RecordingBands { Paint = new SolidColorPaint(SKColors.Red) };
        try
        {
            var xAxis = new Axis { MinLimit = 0, MaxLimit = 100 };
            LiveCharts.Configure(s => s.HasProvider(new FakeEngine(bands) { Target = xAxis }));

            var chart = NewChart(xAxis);
            _ = CoreObjectsTests.ChangingPaintTasks.DrawChart(chart);

            using var surface = SKSurface.Create(new SKImageInfo(600, 400));
            surface.Canvas.Clear(SKColors.White);
            chart.CoreCanvas.DrawFrame(
                new LiveChartsCore.SkiaSharpView.Drawing.SkiaSharpDrawingContext(
                    chart.CoreCanvas, surface.Canvas, SKColor.Empty));

            using var image = surface.Snapshot();
            using var bitmap = SKBitmap.FromImage(image);
            var red = 0;
            for (var x = 0; x < bitmap.Width; x += 2)
                for (var y = 0; y < bitmap.Height; y += 2)
                {
                    var c = bitmap.GetPixel(x, y);
                    if (c.Red > 200 && c.Green < 100 && c.Blue < 100) red++;
                }

            Assert.IsTrue(red > 0, "the bands must rasterize (an Undefined PaintStyle draws nothing)");
        }
        finally
        {
            LiveCharts.Configure(s => s.AddSkiaSharp());
        }
    }

    [TestMethod]
    public void Bands_AnimateOut_WhenTheOverrideStopsReturningThem()
    {
        var bands = new RecordingBands();
        try
        {
            var xAxis = new Axis { MinLimit = 0, MaxLimit = 100 };
            LiveCharts.Configure(s => s.HasProvider(new FakeEngine(bands) { Target = xAxis }));

            var chart = NewChart(xAxis);
            _ = CoreObjectsTests.ChangingPaintTasks.DrawChart(chart);
            Assert.AreEqual(2, bands.Paint.GetGeometries(chart.CoreCanvas).Count(), "bands drawn while enabled");

            bands.Enabled = false;
            _ = CoreObjectsTests.ChangingPaintTasks.DrawChart(chart);

            Assert.AreEqual(
                0, bands.Paint.GetGeometries(chart.CoreCanvas).Count(),
                "bands must detach once the override stops returning them");
        }
        finally
        {
            LiveCharts.Configure(s => s.AddSkiaSharp());
        }
    }

    [TestMethod]
    public void UnmatchedAxis_DrawsNoBands()
    {
        var bands = new RecordingBands();
        try
        {
            // The engine matches no axis, so the override is never consulted and nothing draws.
            LiveCharts.Configure(s => s.HasProvider(new FakeEngine(bands) { Target = null }));

            var chart = NewChart(new Axis { MinLimit = 0, MaxLimit = 100 });
            _ = CoreObjectsTests.ChangingPaintTasks.DrawChart(chart);

            Assert.IsFalse(bands.Consulted, "the override must NOT be consulted for axes the engine does not match");
            Assert.AreEqual(0, bands.Paint.GetGeometries(chart.CoreCanvas).Count(), "no bands are drawn");
        }
        finally
        {
            LiveCharts.Configure(s => s.AddSkiaSharp());
        }
    }
}
