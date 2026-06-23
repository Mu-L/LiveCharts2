using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace CoreTests.CoreObjectsTests;

[TestClass]
public class AxisZeroLineZIndexTests
{
    [TestMethod]
    public void ZeroLineDrawsAboveSeparatorsAndSubseparators()
    {
        // The zero line shared the separators' z-index (-1), so it was hidden behind the grid.
        // After measure the paints carry their default z-index; the zero line must win.
        var yAxis = new Axis
        {
            MinLimit = -10,
            MaxLimit = 10,
            SeparatorsPaint = new SolidColorPaint(SKColors.Gray),
            SubseparatorsPaint = new SolidColorPaint(SKColors.LightGray),
            ZeroPaint = new SolidColorPaint(SKColors.Black),
        };

        var chart = new SKCartesianChart
        {
            Width = 400,
            Height = 400,
            Series = [new LineSeries<double> { Values = [-5, 0, 5] }],
            YAxes = [yAxis],
        };

        _ = chart.GetImage();

        Assert.IsTrue(yAxis.ZeroPaint!.ZIndex > yAxis.SeparatorsPaint!.ZIndex,
            "the zero line must draw above the separators");
        Assert.IsTrue(yAxis.ZeroPaint!.ZIndex > yAxis.SubseparatorsPaint!.ZIndex,
            "the zero line must draw above the subseparators");
    }
}
