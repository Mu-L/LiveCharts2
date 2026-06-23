using System.Linq;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.ImageFilters;
using LiveChartsCore.SkiaSharpView.SKCharts;
using SkiaSharp;

namespace SnapshotTests;

[TestClass]
public sealed class DropShadowFloorTests
{
    // When a paint carries its own drop shadow (e.g. an always-on glow), a per-element shadow
    // that is weaker than it must render AS the paint's shadow — the floor. This is what keeps a
    // hover-grown per-point shadow from fading to nothing (and popping back to the paint shadow)
    // as it animates away. So a weak per-element shadow must look identical to no per-element
    // shadow at all (which already shows the paint shadow).
    [TestMethod]
    public void WeakPerElementShadowIsFlooredAtThePaintShadow()
    {
        SKImage Render(LvcDropShadow? geometryShadow)
        {
            var series = new ColumnSeries<int>
            {
                Values = [5],
                Stroke = null,
                Fill = new SolidColorPaint(SKColors.Red)
                {
                    // an offset base shadow, so the floor must also pin Dx/Dy, not just radius/color.
                    ImageFilter = new DropShadow(8, 8, 20, 20, SKColors.Red)
                },
            };

            var chart = new SKCartesianChart
            {
                Series = [series],
                XAxes = [new Axis()],
                YAxes = [new Axis()],
                Width = 300,
                Height = 300,
            };

            _ = chart.GetImage(); // first measure creates the point geometry
            series.everFetched.First().Context.Visual!.DropShadow = geometryShadow;
            return chart.GetImage();
        }

        // a) no per-element shadow → the paint's drop shadow shows.
        using var paintShadowOnly = Render(null);
        // b) a per-element shadow far weaker than the paint's (a faded hover tail).
        using var weakElementShadow = Render(new LvcDropShadow(0, 0, 1, 1, new LvcColor(255, 0, 0, 30)));

        var result = Extensions.Compare(
            paintShadowOnly, weakElementShadow, perChannelTolerance: 4, maxDifferentPixelsRatio: 0.005);

        Assert.IsTrue(
            result.IsSuccessful,
            "a per-element shadow weaker than the paint's shadow must render as the paint's shadow: " + result.Message);
    }
}
