using System.Collections.ObjectModel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.SKCharts;
using SkiaSharp;

namespace SnapshotTests;

[TestClass]
public sealed class LineSeriesTests
{
    [TestMethod]
    public void Basic()
    {
        var chart = new SKCartesianChart
        {
            Series = [
                new LineSeries<int> {Values = [1, 5, 7, 3]},
                new LineSeries<int> {Values = [4, 2, 8, 6]},
                new LineSeries<int> {Values = [2, 6, 4, 8]}
            ],
            Width = 600,
            Height = 600
        };

        chart.AssertSnapshotMatches($"{nameof(LineSeriesTests)}_{nameof(Basic)}");
    }

    [TestMethod]
    public void Area()
    {
        var chart = new SKCartesianChart
        {
            Series = [
                new LineSeries<int>
                {
                    Values = [1, 5, 7, 3],
                    Fill = new SolidColorPaint(SKColor.Parse("#6495ED")),
                    Stroke = null,
                    GeometryFill = null,
                    GeometryStroke = null
                }
            ],
            Width = 600,
            Height = 600
        };

        chart.AssertSnapshotMatches($"{nameof(LineSeriesTests)}_{nameof(Area)}");
    }

    [TestMethod]
    public void Straight()
    {
        var chart = new SKCartesianChart
        {
            Series = [
                new LineSeries<int>
                {
                    Values = [0, 10, 0, 10, 0],
                    LineSmoothness = 0,
                }
            ],
            Width = 600,
            Height = 600
        };

        chart.AssertSnapshotMatches($"{nameof(LineSeriesTests)}_{nameof(Straight)}");
    }

    [TestMethod]
    public void Curved()
    {
        var chart = new SKCartesianChart
        {
            Series = [
                new LineSeries<int>
                {
                    Values = [0, 10, 0, 10, 0],
                    LineSmoothness = 1,
                }
            ],
            Width = 600,
            Height = 600
        };

        chart.AssertSnapshotMatches($"{nameof(LineSeriesTests)}_{nameof(Curved)}");
    }

    [TestMethod]
    public void Gaps()
    {
        var points = new ObservableCollection<int?>([1, 1]);

        var chart = new SKCartesianChart
        {
            Series = [
                new LineSeries<int?>
                {
                    Values = points,
                    Fill = null
                }
            ],
            Width = 600,
            Height = 600
        };

        _ = chart.GetImage();

        var count = 0;
        int?[] toAdd = [null, 1];

        void Push()
        {
            points.Add(toAdd[count++ % toAdd.Length]);
            points.RemoveAt(0);

            _ = chart.GetImage();
        }

        Push();
        Push();
        Push();
        Push();

        chart.AssertSnapshotMatches($"{nameof(LineSeriesTests)}_{nameof(Gaps)}");
    }
}
