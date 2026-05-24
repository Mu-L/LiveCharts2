using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.SKCharts;
using SkiaSharp;

namespace SnapshotTests;

[TestClass]
public sealed class SankeySeriesTests
{
    [TestMethod]
    public void TwoLevelFlow()
    {
        // Three sources flow into two sinks with mixed weights. Pins the
        // d3-sankey core: depth assignment, column placement, sweep relaxation,
        // and per-link Y stacking at both ends.
        var sources = new[] { new SankeyNode("A"), new SankeyNode("B"), new SankeyNode("C") };
        var sinks = new[] { new SankeyNode("X"), new SankeyNode("Y") };

        var nodes = sources.Concat(sinks).ToArray();
        var links = new[]
        {
            new SankeyLink<SankeyNode>(sources[0], sinks[0], 8),
            new SankeyLink<SankeyNode>(sources[0], sinks[1], 4),
            new SankeyLink<SankeyNode>(sources[1], sinks[0], 6),
            new SankeyLink<SankeyNode>(sources[1], sinks[1], 2),
            new SankeyLink<SankeyNode>(sources[2], sinks[1], 10),
        };

        var chart = new SKSankeyChart
        {
            Series = [
                new SankeySeries<SankeyNode>
                {
                    Values = nodes,
                    Links = links,
                    NodeWidth = 16,
                    Fill = new SolidColorPaint(new SKColor(96, 138, 218)),
                    LinkFill = new SolidColorPaint(new SKColor(96, 138, 218, 90)),
                }
            ],
            Width = 600,
            Height = 400,
        };

        chart.AssertSnapshotMatches($"{nameof(SankeySeriesTests)}_{nameof(TwoLevelFlow)}");
    }

    [TestMethod]
    public void WithLabels()
    {
        // DataLabelsPaint opt-in + auto-wired SankeyNode.Name. The label-side
        // heuristic places labels right-of-node for the left column (A/B/C)
        // and left-of-node for the right column (X/Y), keeping text out of
        // the ribbon area on both sides.
        var sources = new[] { new SankeyNode("Alice"), new SankeyNode("Bob"), new SankeyNode("Carol") };
        var sinks = new[] { new SankeyNode("Trips"), new SankeyNode("Other") };

        var nodes = sources.Concat(sinks).ToArray();
        var links = new[]
        {
            new SankeyLink<SankeyNode>(sources[0], sinks[0], 8),
            new SankeyLink<SankeyNode>(sources[0], sinks[1], 4),
            new SankeyLink<SankeyNode>(sources[1], sinks[0], 6),
            new SankeyLink<SankeyNode>(sources[1], sinks[1], 2),
            new SankeyLink<SankeyNode>(sources[2], sinks[1], 10),
        };

        var chart = new SKSankeyChart
        {
            Series = [
                new SankeySeries<SankeyNode>
                {
                    Values = nodes,
                    Links = links,
                    NodeWidth = 16,
                    Fill = new SolidColorPaint(new SKColor(96, 138, 218)),
                    LinkFill = new SolidColorPaint(new SKColor(96, 138, 218, 90)),
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                    DataLabelsSize = 16,
                }
            ],
            Width = 700,
            Height = 400,
        };

        chart.AssertSnapshotMatches($"{nameof(SankeySeriesTests)}_{nameof(WithLabels)}");
    }

    [TestMethod]
    public void ThreeColumnsWithBranching()
    {
        // Three columns (A,B,C -> M,N -> P,Q) — exercises depth > 2 and the
        // relaxation loop on the middle column.
        var a = new SankeyNode("A");
        var b = new SankeyNode("B");
        var c = new SankeyNode("C");
        var m = new SankeyNode("M");
        var n = new SankeyNode("N");
        var p = new SankeyNode("P");
        var q = new SankeyNode("Q");

        var nodes = new[] { a, b, c, m, n, p, q };
        var links = new[]
        {
            new SankeyLink<SankeyNode>(a, m, 10),
            new SankeyLink<SankeyNode>(b, m, 5),
            new SankeyLink<SankeyNode>(b, n, 3),
            new SankeyLink<SankeyNode>(c, n, 12),
            new SankeyLink<SankeyNode>(m, p, 8),
            new SankeyLink<SankeyNode>(m, q, 7),
            new SankeyLink<SankeyNode>(n, p, 5),
            new SankeyLink<SankeyNode>(n, q, 10),
        };

        var chart = new SKSankeyChart
        {
            Series = [
                new SankeySeries<SankeyNode>
                {
                    Values = nodes,
                    Links = links,
                    NodeWidth = 14,
                    NodePadding = 12,
                    Fill = new SolidColorPaint(new SKColor(232, 105, 105)),
                    LinkFill = new SolidColorPaint(new SKColor(232, 105, 105, 80)),
                }
            ],
            Width = 700,
            Height = 400,
        };

        chart.AssertSnapshotMatches($"{nameof(SankeySeriesTests)}_{nameof(ThreeColumnsWithBranching)}");
    }
}
