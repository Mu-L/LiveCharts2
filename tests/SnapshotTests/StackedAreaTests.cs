using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;

namespace SnapshotTests;

[TestClass]
public sealed class StackedAreaTests
{
    [TestMethod]
    public void Basic()
    {
        var values1 = new double[] { 3, 2, 3, 5, 3, 4, 6 };
        var values2 = new double[] { 6, 5, 6, 3, 8, 5, 2 };
        var values3 = new double[] { 4, 8, 2, 8, 9, 5, 3 };

        var series = new ISeries[]
        {
            new StackedAreaSeries<double> { Values = values1 },
            new StackedAreaSeries<double> { Values = values2 },
            new StackedAreaSeries<double> { Values = values3 }
        };

        var chart = new SKCartesianChart
        {
            Series = series,
            Width = 600,
            Height = 600
        };

        chart.AssertSnapshotMatches($"{nameof(StackedAreaTests)}_{nameof(Basic)}");
    }

    [TestMethod]
    public void Step()
    {
        var values1 = new double[] { 3, 2, 3, 5, 3, 4, 6 };
        var values2 = new double[] { 6, 5, 6, 3, 8, 5, 2 };
        var values3 = new double[] { 4, 8, 2, 8, 9, 5, 3 };

        var series = new ISeries[]
        {
            new StackedStepAreaSeries<double> { Values = values1 },
            new StackedStepAreaSeries<double> { Values = values2 },
            new StackedStepAreaSeries<double> { Values = values3 }
        };

        var chart = new SKCartesianChart
        {
            Series = series,
            Width = 600,
            Height = 600
        };

        chart.AssertSnapshotMatches($"{nameof(StackedAreaTests)}_{nameof(Step)}");
    }
}
