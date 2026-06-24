using System;
using LiveChartsCore.Motion;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.ImageFilters;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace CoreTests.OtherTests;

[TestClass]
public class ImageFiltersTests
{
    [TestMethod]
    public void Blur_CreateNativeBuildsAnSKImageFilter()
    {
        var blur = new Blur(2f, 3f);

        // the filter holds no native; it builds a fresh one that the caller (the paint) owns.
        using var native = blur.CreateNative();
        Assert.IsNotNull(native);
    }

    [TestMethod]
    public void DropShadow_CreateNativeBuildsAnSKImageFilter()
    {
        var dropShadow = new DropShadow(1f, 2f, 3f, 4f, SKColors.Black);

        using var native = dropShadow.CreateNative();
        Assert.IsNotNull(native);
    }

    [TestMethod]
    public void Merge_CreateNativeBuildsAMergedFilter()
    {
        var merged = new ImageFiltersMergeOperation(
            [new Blur(2f, 2f), new DropShadow(1f, 1f, 1f, 1f, SKColors.Black)]);

        // builds each child native, merges them, and returns a single owned native (the transient
        // child handles are released internally — the merge holds its own references).
        using var native = merged.CreateNative();
        Assert.IsNotNull(native);
    }

    [TestMethod]
    public void Merge_StaticTransitionateBlendsBetweenSameLengthOperations()
    {
        var from = new ImageFiltersMergeOperation([new Blur(0f, 0f), new Blur(0f, 0f)]);
        var to = new ImageFiltersMergeOperation([new Blur(10f, 10f), new Blur(20f, 20f)]);

        // Internal static helper used by the animation pipeline.
        var blended = (ImageFiltersMergeOperation?)ImageFilter.Transitionate(from, to, 0.5f);
        Assert.IsNotNull(blended);
    }

    [TestMethod]
    public void Merge_TransitionateMismatchedLengthThrows()
    {
        var from = new ImageFiltersMergeOperation([new Blur(0f, 0f)]);
        var to = new ImageFiltersMergeOperation([new Blur(1f, 1f), new Blur(2f, 2f)]);

        Assert.ThrowsExactly<Exception>(() => _ = ImageFilter.Transitionate(from, to, 0.5f));
    }

    [TestMethod]
    public void StaticTransitionateNullToNullReturnsNull()
    {
        Assert.IsNull(ImageFilter.Transitionate(null, null, 0.5f));
    }

    [TestMethod]
    public void StaticTransitionateNullSideUsesRegisteredDefault()
    {
        // When one side is null, the static helper falls back to a default registered
        // by the filter's key (e.g. transparent shadow / zero blur).
        var blur = new Blur(5f, 5f);

        var fromNull = ImageFilter.Transitionate(null, blur, 0.25f);
        Assert.IsNotNull(fromNull);

        var toNull = ImageFilter.Transitionate(blur, null, 0.25f);
        Assert.IsNotNull(toNull);
    }

    [TestMethod]
    public void DropShadow_TransitionateBlendsBetweenSameType()
    {
        // Calls the static helper which in turn dispatches to DropShadow.Transitionate.
        var from = new DropShadow(0f, 0f, 0f, 0f, SKColors.Black);
        var to = new DropShadow(10f, 20f, 5f, 5f, SKColors.Red);

        var blended = (DropShadow?)ImageFilter.Transitionate(from, to, 0.5f);

        Assert.IsNotNull(blended);
        Assert.AreNotSame(from, blended);
        Assert.AreNotSame(to, blended);
    }

    [TestMethod]
    public void Filter_NativeIsRebuilt_WhenPaintIsReusedAfterDispose()
    {
        // The paint owns and caches the native filter keyed on the source instance. After the paint
        // is disposed (e.g. between renders) the cached native is gone, but the SAME filter instance
        // is still assigned — the next paint pass must rebuild the native, not reuse the disposed one.
        // Regression: forgetting to clear the source on dispose made the rebuild be skipped, dropping
        // the filter entirely on the second render.
        using var surface = SKSurface.Create(new SKImageInfo(10, 10));
        var canvas = new CoreMotionCanvas();
        var context = new SkiaSharpDrawingContext(canvas, surface.Canvas, SKColor.Empty);

        var paint = new SolidColorPaint(SKColors.Red)
        {
            ImageFilter = new DropShadow(2f, 2f, 4f, 4f, SKColors.Black)
        };

        paint.OnPaintStarted(context, null);
        Assert.IsNotNull(paint._skiaPaint!.ImageFilter, "the native filter must be built on first use.");

        paint.DisposeTask();

        paint.OnPaintStarted(context, null);
        Assert.IsNotNull(
            paint._skiaPaint!.ImageFilter,
            "the native filter must be rebuilt after the paint was disposed, not left null.");
    }

    [TestMethod]
    public void DropShadow_AppliedThroughChartRendersWithoutError()
    {
        var paint = new SolidColorPaint(SKColors.Red, 6)
        {
            ImageFilter = new DropShadow(2f, 2f, 4f, 4f, SKColors.Black)
        };

        var chart = new SKCartesianChart
        {
            Width = 200,
            Height = 200,
            Series = [
                new LineSeries<double> { Values = [1, 2, 3], Stroke = paint, Fill = null }
            ]
        };

        using var image = chart.GetImage();
        Assert.IsNotNull(image);
    }
}
