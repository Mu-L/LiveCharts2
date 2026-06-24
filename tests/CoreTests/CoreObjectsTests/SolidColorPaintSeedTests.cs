using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace CoreTests.CoreObjectsTests;

// A SolidColorPaint must seed its Color as the motion property's baseline (From == To == the color),
// not assign it through the setter (which would leave the baseline at the type default and treat the
// constructed color as an animation target). Covers the single-arg ctor and CloneTask.
[TestClass]
public class SolidColorPaintSeedTests
{
    [TestMethod]
    public void Constructor_And_Clone_SeedColorAsBaseline()
    {
        var red = new SKColor(255, 0, 0);

        var paint = new SolidColorPaint(red);
        var motion = paint.GetPropertyDefinition(nameof(SolidColorPaint.Color))!.GetMotion(paint)!;

        Assert.AreEqual(red, (SKColor)motion.FromValue!, "the ctor must seed the color as the baseline (From)");
        Assert.AreEqual(red, (SKColor)motion.ToValue!);

        var clone = (SolidColorPaint)paint.CloneTask();
        var cloneMotion = clone.GetPropertyDefinition(nameof(SolidColorPaint.Color))!.GetMotion(clone)!;

        Assert.AreEqual(red, (SKColor)cloneMotion.FromValue!, "the clone must seed the color as the baseline too");
        Assert.AreEqual(red, (SKColor)cloneMotion.ToValue!);
    }
}
