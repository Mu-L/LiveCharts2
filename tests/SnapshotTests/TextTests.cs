using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using SkiaSharp;

namespace SnapshotTests;

[TestClass]
public sealed class TextTests
{
    [TestMethod]
    public void MultilineText()
    {
        var label = $"Hi this is a label with {Environment.NewLine}a long text that generates {Environment.NewLine}multi lines";

        var chart = new SKCartesianChart
        {
            Series = [
                new LineSeries<double> { Values = [1, 2, 3] }
            ],
            XAxes = [
                new Axis
                {
                    LabelsRotation = 45,
                    Labels = [
                        label,
                        label,
                        label
                    ],
                }
            ],
            YAxes = [
                new Axis
                {
                }
            ],
            Width = 600,
            Height = 600
        };

        chart.AssertSnapshotMatches($"{nameof(TextTests)}_{nameof(MultilineText)}");
    }

    [TestMethod]
    public void MultilineTextInTooltips()
    {
        var label = $"Hi this is a label with {Environment.NewLine}a long text that generates {Environment.NewLine}multi lines";

        var chart = new SKCartesianChart
        {
            Series = [
                new LineSeries<double> { Values = [1, 2, 3] }
            ],
            XAxes = [
                new Axis
                {
                    LabelsRotation = 45,
                    Labels = [
                        label,
                        label,
                        label
                    ],
                }
            ],
            YAxes = [
                new Axis
                {
                }
            ],
            Width = 600,
            Height = 600
        };

        chart.PointerAt(300, 300);
        chart.AssertSnapshotMatches($"{nameof(TextTests)}_{nameof(MultilineTextInTooltips)}");
    }

    [TestMethod]
    public void RenderUnshapedGlyphs()
    {
        var chart = new SKCartesianChart
        {
            Series = [
                new LineSeries<double> { Values = [1, 2, 3] }
            ],
            XAxes = [
                new Axis
                {
                    Labels = [ "王", "赵", "张" ],
                    TextSize = 50,
                }
            ],
            YAxes = [
                new Axis
                {

                }
            ],
            Width = 600,
            Height = 600
        };

        chart.AssertSnapshotMatches($"{nameof(TextTests)}_{nameof(RenderUnshapedGlyphs)}");
    }

    [TestMethod]
    public void RenderShapedGlyphs()
    {
        var label = "مرحبا بالعالم";
        var values = new double[] { 1, 2, 3 };
        var typeface = SKFontManager.Default.MatchCharacter('أ');

        var chart = new SKCartesianChart
        {
            Series = [
                new ColumnSeries<double> { Values = values },
            ],
            XAxes = [
                new Axis
                {
                    TextSize = 30,
                    LabelsRotation = 45,
                    Labels = [
                        label,
                        label,
                        label
                    ]
                }
            ],
            Width = 600,
            Height = 600
        };

        chart.AssertSnapshotMatches($"{nameof(TextTests)}_{nameof(RenderShapedGlyphs)}");
    }

    [TestMethod]
    public void RenderShapedGlyphsMultiLine()
    {
        var label =
            "هذا نص طويل" + Environment.NewLine +
            "يحتوي على عدة أسطر" + Environment.NewLine +
            "مكتوب باللغة العربية";
        var values = new double[] { 1, 2, 3 };
        var typeface = SKFontManager.Default.MatchCharacter('أ');

        var chart = new SKCartesianChart
        {
            Series = [
                new ColumnSeries<double> { Values = values },
            ],
            XAxes = [
                new Axis
                {
                    TextSize = 30,
                    LabelsRotation = 45,
                    Labels = [
                        label,
                        label,
                        label
                    ]
                }
            ],
            Width = 800,
            Height = 600
        };

        chart.AssertSnapshotMatches($"{nameof(TextTests)}_{nameof(RenderShapedGlyphsMultiLine)}");
    }

    [TestMethod]
    public void RenderShapedGlyphsMultiLineInTooltips()
    {
        var label =
            "هذا نص طويل" + Environment.NewLine +
            "يحتوي على عدة أسطر" + Environment.NewLine +
            "مكتوب باللغة العربية";
        var values = new double[] { 1, 2, 3 };
        var typeface = SKFontManager.Default.MatchCharacter('أ');

        var chart = new SKCartesianChart
        {
            Series = [
                new ColumnSeries<double> { Values = values },
            ],
            XAxes = [
                new Axis
                {
                    TextSize = 30,
                    LabelsRotation = 45,
                    Labels = [
                        label,
                        label,
                        label
                    ]
                }
            ],
            Width = 800,
            Height = 600
        };

        chart.PointerAt(200, 100);
        chart.AssertSnapshotMatches($"{nameof(TextTests)}_{nameof(RenderShapedGlyphsMultiLineInTooltips)}");
    }
}
