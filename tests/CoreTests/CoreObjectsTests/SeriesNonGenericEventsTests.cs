using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Drawing;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests.CoreObjectsTests;

// The strongly-typed Series<TModel, TVisual, TLabel> events (PointMeasured, PointCreated,
// DataPointerDown, ChartPointPointerHover/HoverLost/Down) each have a non-generic twin on
// ISeries so code that only holds an ISeries reference (e.g. a theme rule) can subscribe
// without knowing the generic arguments. These tests pin that the twins fire through the
// same On* paths as their typed counterparts, and that both fire side by side.
[TestClass]
public class SeriesNonGenericEventsTests
{
    [TestMethod]
    public void PointMeasured_NonGeneric_FiresOncePerPointWithEntityIndex()
    {
        var series = new ColumnSeries<int> { Values = [10, 20, 30] };
        var chart = NewChart(series);

        var indices = new List<int>();
        ((ISeries)series).PointMeasured += p => indices.Add(p.Context.Entity.MetaData!.EntityIndex);

        _ = ChangingPaintTasks.DrawChart(chart);

        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, indices,
            "the non-generic PointMeasured must fire once per point, in entity-index order");
    }

    [TestMethod]
    public void PointMeasured_NonGenericAndTyped_BothFire()
    {
        var series = new ColumnSeries<int> { Values = [10, 20, 30] };
        var chart = NewChart(series);

        var typed = 0;
        var nonGeneric = 0;
        series.PointMeasured += _ => typed++;
        ((ISeries)series).PointMeasured += _ => nonGeneric++;

        _ = ChangingPaintTasks.DrawChart(chart);

        Assert.AreEqual(3, typed, "typed PointMeasured must keep firing");
        Assert.AreEqual(3, nonGeneric, "non-generic PointMeasured must fire alongside the typed one");
    }

    [TestMethod]
    public void PointCreated_NonGeneric_FiresPerPoint()
    {
        var series = new ColumnSeries<int> { Values = [10, 20, 30] };
        var chart = NewChart(series);

        var created = 0;
        ((ISeries)series).PointCreated += _ => created++;

        _ = ChangingPaintTasks.DrawChart(chart);

        Assert.AreEqual(3, created);
    }

    [TestMethod]
    public void DataPointerDown_NonGeneric_FiresWithHitPoints()
    {
        var series = new ColumnSeries<int> { Values = [10, 20, 30] };
        var chart = NewChart(series);

        IEnumerable<ChartPoint>? received = null;
        IChartView? sender = null;
        ((ISeries)series).DataPointerDown += (c, pts) => { sender = c; received = pts; };

        _ = ChangingPaintTasks.DrawChart(chart);
        chart.CoreChart.InvokePointerDown(FirstPointCenter(series, chart), isSecondaryAction: false);

        Assert.IsNotNull(received);
        Assert.IsTrue(received!.Any(), "expected at least one hit point on the non-generic series event");
        Assert.AreSame(chart, sender);
    }

    [TestMethod]
    public void ChartPointPointerDown_NonGeneric_FiresWithClosestPoint()
    {
        var series = new ColumnSeries<int> { Values = [10, 20, 30] };
        var chart = NewChart(series);

        ChartPoint? closest = null;
        ((ISeries)series).ChartPointPointerDown += (_, p) => closest = p;

        _ = ChangingPaintTasks.DrawChart(chart);
        chart.CoreChart.InvokePointerDown(FirstPointCenter(series, chart), isSecondaryAction: false);

        Assert.IsNotNull(closest);
    }

    [TestMethod]
    public void RequiresFindClosestOnPointerDown_HonorsNonGenericSubscription()
    {
        // InvokePointerDown skips a series whose RequiresFindClosestOnPointerDown is false,
        // so the flag must account for non-generic subscribers or their event never fires.
        var series = new ColumnSeries<int> { Values = [10, 20, 30] };

        Assert.IsFalse(((ISeries)series).RequiresFindClosestOnPointerDown);

        ((ISeries)series).ChartPointPointerDown += (_, _) => { };

        Assert.IsTrue(((ISeries)series).RequiresFindClosestOnPointerDown,
            "subscribing to the non-generic ChartPointPointerDown must request find-closest hit testing");
    }

    [TestMethod]
    public async Task ChartPointPointerHover_NonGeneric_FiresOnPointerMove()
    {
        var series = new ColumnSeries<int> { Values = [10, 20, 30] };
        var chart = NewChartWithTooltip(series);

        ChartPoint? hovered = null;
        ((ISeries)series).ChartPointPointerHover += (_, p) => hovered = p;

        _ = ChangingPaintTasks.DrawChart(chart);
        chart.CoreChart.InvokePointerMove(FirstPointCenter(series, chart));
        await chart.CoreChart.TooltipThrottlerUnlocked();

        Assert.IsNotNull(hovered);
    }

    [TestMethod]
    public async Task ChartPointPointerHoverLost_NonGeneric_FiresOnPointerLeft()
    {
        // OnPointerLeft is gated by point.IsPointerOver, which OnPointerEnter only flips when
        // a hover subscriber exists — so subscribe the non-generic hover too, then leave.
        var series = new ColumnSeries<int> { Values = [10, 20, 30] };
        var chart = NewChartWithTooltip(series);

        ChartPoint? lost = null;
        ((ISeries)series).ChartPointPointerHover += (_, _) => { };
        ((ISeries)series).ChartPointPointerHoverLost += (_, p) => lost = p;

        _ = ChangingPaintTasks.DrawChart(chart);
        chart.CoreChart.InvokePointerMove(FirstPointCenter(series, chart));
        await chart.CoreChart.TooltipThrottlerUnlocked();
        chart.CoreChart.InvokePointerLeft();

        Assert.IsNotNull(lost);
    }

    private static SKCartesianChart NewChart(ISeries series) => new()
    {
        Series = [series],
        XAxes = [new Axis()],
        YAxes = [new Axis()],
    };

    private static SKCartesianChart NewChartWithTooltip(ISeries series) => new()
    {
        Series = [series],
        XAxes = [new Axis()],
        YAxes = [new Axis()],
        Tooltip = new SKDefaultTooltip(),
    };

    private static LvcPoint FirstPointCenter(ISeries series, SKCartesianChart chart)
    {
        var first = series.Fetch(chart.CoreChart).First();
        var area = (RectangleHoverArea)first.Context.HoverArea!;
        return new LvcPoint(area.X + area.Width / 2, area.Y + area.Height / 2);
    }
}
