using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;

namespace SnapshotTests;

[TestClass]
public sealed class PolarChartTests
{
    [TestMethod]
    public void Basic()
    {
        var values = new double[] { 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
        var cotangentAngle = LiveCharts.CotangentAngle;
        var tangentAngle = LiveCharts.TangentAngle;

        var series = new ISeries[]
        {
            new PolarLineSeries<double>
            {
                Values = values,
                ShowDataLabels = true,
                GeometrySize = 15,
                DataLabelsSize = 8,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsRotation = cotangentAngle,
                IsClosed = true
            }
        };

        var radiusAxes = new PolarAxis[]
        {
            new() {
                LabelsAngle = -60,
                MaxLimit = 30
            }
        };

        var angleAxes = new PolarAxis[]
        {
            new() {
                LabelsRotation = tangentAngle
            }
        };

        var chart = new SKPolarChart
        {
            Series = series,
            RadiusAxes = radiusAxes,
            AngleAxes = angleAxes,
            Width = 600,
            Height = 600
        };

        chart.AssertSnapshotMatches($"{nameof(PolarChartTests)}_{nameof(Basic)}");
    }
}
