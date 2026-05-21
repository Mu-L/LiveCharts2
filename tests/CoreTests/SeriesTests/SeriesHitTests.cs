using System;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Extensions;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests.SeriesTests;

// Coverage matrix for Series.FindPointsInPosition / ISeries.FindHitPoints
// across every series type and every FindingStrategy. Each series gets:
//   * a "hit at center" probe per supported strategy
//   * a "miss outside any point" probe
//   * one or more contract probes that pin behavior unique to that series
//     (gap padding, ExactMatch precision, axis-only acceptance, etc.)
//
// Probes go through SKChart.GetPointsAt, which is the public path consumers
// touch via DataPointerDown/Hover.
[TestClass]
public class SeriesHitTests
{
    // --- ScatterSeries ----------------------------------------------------
    // Default strategy = CompareAllTakeClosest. RectangleHoverArea is a
    // gs x gs square centred on the point.

    [TestMethod]
    public void Scatter_CompareAll_HitsAtCenter()
    {
        var series = new ScatterSeries<ObservablePoint>
        {
            Values = [new(0, 0), new(1, 1), new(2, 2)],
            GeometrySize = 20,
        };
        var chart = NewCartesianChart(series);
        var center = HoverAreaCenter(series, chart, 1);

        var hits = chart.GetPointsAt(new(center.X, center.Y), FindingStrategy.CompareAll).ToArray();
        Assert.AreEqual(1, hits.Length);
        Assert.AreEqual(1d, hits[0].Coordinate.SecondaryValue);
    }

    [TestMethod]
    public void Scatter_CompareAllTakeClosest_PicksOneOnOverlap()
    {
        var series = new ScatterSeries<ObservablePoint>
        {
            Values = [new(0, 0), new(0.01, 0.01)],
            GeometrySize = 80,
        };
        var chart = NewCartesianChart(series);
        var c0 = HoverAreaCenter(series, chart, 0);

        var hits = chart.GetPointsAt(
            new(c0.X + 1, c0.Y + 1),
            FindingStrategy.CompareAllTakeClosest).ToArray();
        Assert.AreEqual(1, hits.Length, "TakeClosest must collapse the overlap to one point");
    }

    [TestMethod]
    public void Scatter_OutsideArea_Misses()
    {
        var series = new ScatterSeries<ObservablePoint> { Values = [new(1, 1)], GeometrySize = 10 };
        var chart = NewCartesianChart(series);
        _ = chart.GetImage();
        var hits = chart.GetPointsAt(new(0, 0), FindingStrategy.CompareAll).ToArray();
        Assert.AreEqual(0, hits.Length);
    }

    [TestMethod]
    public void Scatter_CompareOnlyX_HitsAnyYInColumn()
    {
        var series = new ScatterSeries<ObservablePoint>
        {
            Values = [new(1, 1)],
            GeometrySize = 20,
        };
        var chart = NewCartesianChart(series);
        _ = chart.GetImage();
        var area = ReadHoverArea(series, chart, 0);
        var midX = area.X + area.Width * 0.5f;

        // Y is far above the marker — X-only must still hit because the area
        // intersects the column on X.
        var hits = chart.GetPointsAt(new(midX, area.Y - 50), FindingStrategy.CompareOnlyX).ToArray();
        Assert.AreEqual(1, hits.Length);
    }

    // --- ColumnSeries (BarSeries with vertical orientation) ---------------
    // Default strategy = CompareOnlyXTakeClosest. HoverArea uses `actualUw`
    // (wider than the visual) so the gap between bars is hoverable.
    // ExactMatch instead probes the *visual* directly via the override.

    [TestMethod]
    public void Column_CompareOnlyX_HitsAtColumnCenter()
    {
        var series = new ColumnSeries<int> { Values = [10, 20, 30] };
        var chart = NewCartesianChart(series);
        var center = HoverAreaCenter(series, chart, 1);

        var hits = chart.GetPointsAt(new(center.X, center.Y), FindingStrategy.CompareOnlyX).ToArray();
        Assert.AreEqual(1, hits.Length);
        Assert.AreEqual(20d, hits[0].Coordinate.PrimaryValue);
    }

    [TestMethod]
    public void Column_CompareOnlyXTakeClosest_VisualGapStillHits()
    {
        // ColumnSeries's HoverArea uses actualUw (the full slot width) while
        // the visual is uw = actualUw - Padding. A probe placed in the
        // visual-only gap (between two column visuals on X) must therefore
        // still be covered by a HoverArea — the "easy to hover" UX contract.
        var series = new ColumnSeries<int> { Values = [10, 20, 30] };
        var chart = NewCartesianChart(series);
        var v0 = ReadVisual<RoundedRectangleGeometry>(series, chart, 0);
        var v1 = ReadVisual<RoundedRectangleGeometry>(series, chart, 1);

        var midGapX = (v0.X + v0.Width + v1.X) * 0.5f;
        var midY = (v0.Y + v1.Y) * 0.5f;

        var hits = chart.GetPointsAt(
            new(midGapX, midY),
            FindingStrategy.CompareOnlyXTakeClosest).ToArray();
        Assert.AreEqual(1, hits.Length, "Column hover area should cover the visual gap (UX contract)");
    }

    [TestMethod]
    public void Column_ExactMatch_HitsInsideVisualOnly()
    {
        var series = new ColumnSeries<int> { Values = [10, 20, 30] };
        var chart = NewCartesianChart(series);
        var v = ReadVisual<RoundedRectangleGeometry>(series, chart, 1);
        var centerX = v.X + v.Width * 0.5f;
        var centerY = v.Y + v.Height * 0.5f;

        var hits = chart.GetPointsAt(new(centerX, centerY), FindingStrategy.ExactMatch).ToArray();
        Assert.AreEqual(1, hits.Length);
        Assert.AreEqual(20d, hits[0].Coordinate.PrimaryValue);
    }

    [TestMethod]
    public void Column_ExactMatch_GapBetweenColumnsMisses()
    {
        // ExactMatch override must use the visual extents, not the wider
        // actualUw hover area. A probe in the gap between two visuals must
        // miss — otherwise ExactMatch degrades to CompareOnlyX behavior.
        var series = new ColumnSeries<int> { Values = [10, 20, 30] };
        var chart = NewCartesianChart(series);
        var v0 = ReadVisual<RoundedRectangleGeometry>(series, chart, 0);
        var v1 = ReadVisual<RoundedRectangleGeometry>(series, chart, 1);

        var gapX = (v0.X + v0.Width + v1.X) * 0.5f;
        var midY = (v0.Y + v1.Y) * 0.5f;

        var hits = chart.GetPointsAt(new(gapX, midY), FindingStrategy.ExactMatch).ToArray();
        Assert.AreEqual(0, hits.Length,
            "ExactMatch must respect visual edges, not the inflated hover area");
    }

    [TestMethod]
    public void Column_OutsideArea_Misses()
    {
        var series = new ColumnSeries<int> { Values = [10, 20, 30] };
        var chart = NewCartesianChart(series);
        _ = chart.GetImage();
        var hits = chart.GetPointsAt(new(-50, -50), FindingStrategy.CompareOnlyXTakeClosest).ToArray();
        Assert.AreEqual(0, hits.Length);
    }

    // --- RowSeries (horizontal Bar) --------------------------------------
    // Default strategy = CompareOnlyYTakeClosest.

    [TestMethod]
    public void Row_CompareOnlyY_HitsAtRowCenter()
    {
        var series = new RowSeries<int> { Values = [10, 20, 30] };
        var chart = NewCartesianChart(series);
        var center = HoverAreaCenter(series, chart, 1);

        var hits = chart.GetPointsAt(new(center.X, center.Y), FindingStrategy.CompareOnlyY).ToArray();
        Assert.AreEqual(1, hits.Length);
        Assert.AreEqual(20d, hits[0].Coordinate.PrimaryValue);
    }

    [TestMethod]
    public void Row_CompareOnlyYTakeClosest_VisualGapStillHits()
    {
        // RowSeries inflates its HoverArea height to `actualUw` (the slot
        // height) while the visual height is `uw = actualUw - Padding`.
        // A probe placed just inside the visual-only gap (between two row
        // visuals on Y) must therefore still be covered by a HoverArea.
        // The probe nudges 1 px off the exact midpoint because the gap
        // midpoint lands on the boundary between the two hover areas, where
        // strict-edge IsPointerOver returns false for both.
        var series = new RowSeries<int> { Values = [10, 20, 30] };
        var chart = NewCartesianChart(series);
        var v0 = ReadVisual<RoundedRectangleGeometry>(series, chart, 0);
        var v1 = ReadVisual<RoundedRectangleGeometry>(series, chart, 1);

        var (top, bottom) = v0.Y < v1.Y ? (v0, v1) : (v1, v0);
        var midX = (top.X + bottom.X) * 0.5f;
        var inGapButTowardTop = (top.Y + top.Height + bottom.Y) * 0.5f - 1;

        var hits = chart.GetPointsAt(
            new(midX, inGapButTowardTop),
            FindingStrategy.CompareOnlyYTakeClosest).ToArray();
        Assert.AreEqual(1, hits.Length, "Row hover area should cover the visual inter-row gap (UX contract)");
    }

    // --- BoxSeries -------------------------------------------------------
    // Default = CompareOnlyXTakeClosest. RectangleHoverArea spans the full
    // whisker rectangle (High → Low). Override goes pixel-precise on ExactMatch.

    [TestMethod]
    public void Box_CompareOnlyX_HitsInWhiskerColumn()
    {
        var series = new BoxSeries<BoxValue>
        {
            Values =
            [
                new(10, 8, 5, 3, 1),
                new(20, 18, 15, 13, 11),
                new(30, 28, 25, 23, 21),
            ],
        };
        var chart = NewCartesianChart(series);
        var center = HoverAreaCenter(series, chart, 1);

        var hits = chart.GetPointsAt(new(center.X, center.Y), FindingStrategy.CompareOnlyX).ToArray();
        Assert.AreEqual(1, hits.Length);
    }

    [TestMethod]
    public void Box_ExactMatch_HitsInsideVisual()
    {
        var series = new BoxSeries<BoxValue>
        {
            Values =
            [
                new(10, 8, 5, 3, 1),
                new(20, 18, 15, 13, 11),
                new(30, 28, 25, 23, 21),
            ],
        };
        var chart = NewCartesianChart(series);
        var v = ReadVisual<BoxGeometry>(series, chart, 1);
        var cx = v.X + v.Width * 0.5f;
        var cy = (v.Y + v.Min) * 0.5f;

        var hits = chart.GetPointsAt(new(cx, cy), FindingStrategy.ExactMatch).ToArray();
        Assert.AreEqual(1, hits.Length);
    }

    // --- CandlesticksSeries (Financial) ----------------------------------
    // Default = CompareOnlyXTakeClosest.

    [TestMethod]
    public void Candlestick_CompareOnlyX_HitsInCandleColumn()
    {
        var series = new CandlesticksSeries<FinancialPointI>
        {
            Values =
            [
                new(10, 8, 5, 3),
                new(20, 18, 15, 13),
                new(30, 28, 25, 23),
            ],
        };
        var chart = NewCartesianChart(series);
        var center = HoverAreaCenter(series, chart, 1);

        var hits = chart.GetPointsAt(new(center.X, center.Y), FindingStrategy.CompareOnlyX).ToArray();
        Assert.AreEqual(1, hits.Length);
    }

    [TestMethod]
    public void Candlestick_CompareOnlyX_GapAroundVisualStillHits()
    {
        // MaxBarWidth (default 25) clamps the visual candle to a thin column
        // inside its axis-unit slot. The hover area must cover the whole
        // slot so a probe in the "gap" between visuals (still inside the
        // category column) still hits — matching BoxSeries / BarSeries.
        // Pre-fix the hover area was the visual width and this probe missed.
        var series = new CandlesticksSeries<FinancialPointI>
        {
            Values = [new(10, 8, 5, 3), new(20, 18, 15, 13)],
        };
        var chart = NewCartesianChart(series);
        var v = ReadVisual<CandlestickGeometry>(series, chart, 0);

        // 5 px past the right edge of the (narrow) candle body — clearly
        // outside the visual but well inside the axis-unit slot on a
        // 1000 px wide 2-candle chart.
        var probeX = v.X + v.Width + 5;
        var probeY = v.Y + 1;

        var hits = chart.GetPointsAt(new(probeX, probeY), FindingStrategy.CompareOnlyX).ToArray();
        Assert.AreEqual(1, hits.Length,
            "Candlestick hover area should span the full category slot, not just the clamped visual");
    }

    // --- LineSeries ------------------------------------------------------
    // Default = CompareOnlyXTakeClosest. HoverArea is a uwx × geometrySize
    // rectangle around the marker. ExactMatch goes through the override and
    // checks the visual marker rectangle only.

    [TestMethod]
    public void Line_CompareOnlyX_HitsAnyYInColumn()
    {
        var series = new LineSeries<int> { Values = [10, 20, 30], GeometrySize = 12 };
        var chart = NewCartesianChart(series);
        var area = ReadHoverArea(series, chart, 1);
        var midX = area.X + area.Width * 0.5f;
        var probeY = area.Y - 100;

        var hits = chart.GetPointsAt(new(midX, probeY), FindingStrategy.CompareOnlyX).ToArray();
        Assert.AreEqual(1, hits.Length);
        Assert.AreEqual(20d, hits[0].Coordinate.PrimaryValue);
    }

    [TestMethod]
    public void Line_ExactMatch_MissesOffMarker()
    {
        // ExactMatch on LineSeries goes through the override and pins the
        // hit to the marker rectangle only. A probe within the column but
        // far from the marker on Y must miss.
        var series = new LineSeries<int> { Values = [10, 20, 30], GeometrySize = 12 };
        var chart = NewCartesianChart(series);
        var area = ReadHoverArea(series, chart, 1);
        var midX = area.X + area.Width * 0.5f;
        var probeY = area.Y - 100;

        var hits = chart.GetPointsAt(new(midX, probeY), FindingStrategy.ExactMatch).ToArray();
        Assert.AreEqual(0, hits.Length);
    }

    [TestMethod]
    public void Line_ExactMatchTakeClosest_EmptyProbeReturnsNothing()
    {
        // ExactMatchTakeClosest must still respect the visual-containment
        // filter: a probe far from any marker returns empty, not the
        // nearest marker. Pre-fix the override skipped the filter and
        // unconditionally returned one point.
        var series = new LineSeries<int> { Values = [10, 20, 30], GeometrySize = 12 };
        var chart = NewCartesianChart(series);
        _ = chart.GetImage();

        var hits = chart.GetPointsAt(new(0, 0), FindingStrategy.ExactMatchTakeClosest).ToArray();
        Assert.AreEqual(0, hits.Length);
    }

    [TestMethod]
    public void Line_ExactMatchTakeClosest_OnMarker_ReturnsOne()
    {
        // A probe at the marker center DOES hit (one closest point).
        var series = new LineSeries<int> { Values = [10, 20, 30], GeometrySize = 12 };
        var chart = NewCartesianChart(series);
        var v = ReadVisual<CircleGeometry>(series, chart, 1);
        var cx = v.X + v.TranslateTransform.X + v.Width * 0.5f;
        var cy = v.Y + v.TranslateTransform.Y + v.Height * 0.5f;

        var hits = chart.GetPointsAt(new(cx, cy), FindingStrategy.ExactMatchTakeClosest).ToArray();
        Assert.AreEqual(1, hits.Length);
    }

    // --- StepLineSeries --------------------------------------------------
    // Mirrors LineSeries with the same override pattern.

    [TestMethod]
    public void StepLine_CompareOnlyX_HitsAnyYInColumn()
    {
        var series = new StepLineSeries<int> { Values = [10, 20, 30], GeometrySize = 12 };
        var chart = NewCartesianChart(series);
        var area = ReadHoverArea(series, chart, 1);
        var midX = area.X + area.Width * 0.5f;
        var probeY = area.Y - 100;

        var hits = chart.GetPointsAt(new(midX, probeY), FindingStrategy.CompareOnlyX).ToArray();
        Assert.AreEqual(1, hits.Length);
    }

    [TestMethod]
    public void StepLine_ExactMatchTakeClosest_EmptyProbeReturnsNothing()
    {
        var series = new StepLineSeries<int> { Values = [10, 20, 30], GeometrySize = 12 };
        var chart = NewCartesianChart(series);
        _ = chart.GetImage();

        var hits = chart.GetPointsAt(new(0, 0), FindingStrategy.ExactMatchTakeClosest).ToArray();
        Assert.AreEqual(0, hits.Length);
    }

    // --- HeatSeries ------------------------------------------------------
    // Default = CompareAllTakeClosest (PrefersXYStrategyTooltips falls through).
    // Each cell carries a RectangleHoverArea covering exactly the cell rect.

    [TestMethod]
    public void Heat_CompareAll_HitsCellCenter()
    {
        var series = new HeatSeries<WeightedPoint>
        {
            Values =
            [
                new(0, 0, 0), new(0, 1, 1), new(0, 2, 2),
                new(1, 0, 3), new(1, 1, 4), new(1, 2, 5),
                new(2, 0, 6), new(2, 1, 7), new(2, 2, 8),
            ],
        };
        var chart = NewCartesianChart(series);
        var center = HoverAreaCenter(series, chart, 4); // cell (1,1)

        var hits = chart.GetPointsAt(new(center.X, center.Y), FindingStrategy.CompareAll).ToArray();
        Assert.AreEqual(1, hits.Length);
    }

    [TestMethod]
    public void Heat_OutsideAnyCell_Misses()
    {
        var series = new HeatSeries<WeightedPoint> { Values = [new(0, 0, 0)] };
        var chart = NewCartesianChart(series);
        _ = chart.GetImage();
        var hits = chart.GetPointsAt(new(-100, -100), FindingStrategy.CompareAll).ToArray();
        Assert.AreEqual(0, hits.Length);
    }

    // --- PolarLineSeries -------------------------------------------------
    // Like ScatterSeries: square HoverArea around each polar point.

    [TestMethod]
    public void PolarLine_CompareAll_HitsAtPointCenter()
    {
        var series = new PolarLineSeries<int> { Values = [10, 20, 30], GeometrySize = 20 };
        var chart = new SKPolarChart
        {
            Width = 600,
            Height = 600,
            Series = [series],
        };
        _ = chart.GetImage();

        var area = (RectangleHoverArea)((ISeries)series)
            .Fetch(chart.CoreChart).ElementAt(1).Context.HoverArea!;
        var center = new LvcPoint(area.X + area.Width * 0.5f, area.Y + area.Height * 0.5f);

        var hits = chart.GetPointsAt(new(center.X, center.Y), FindingStrategy.CompareAll).ToArray();
        Assert.AreEqual(1, hits.Length);
    }

    // --- PieSeries -------------------------------------------------------
    // SemicircleHoverArea: angular containment + radial containment in
    // [InnerRadius, Radius/2].

    [TestMethod]
    public void Pie_HitsInsideSlice()
    {
        var seriesCollection = new double[] { 100, 100, 100, 100 }.AsPieSeries();
        var chart = new SKPieChart
        {
            Width = 400,
            Height = 400,
            Series = seriesCollection,
        };
        _ = chart.GetImage();

        // Slice 0 occupies angles [0, 90); probe at ~45° at mid-radius.
        var cx = 200f; var cy = 200f;
        var r = 80f;
        var angle = 45 * Math.PI / 180;
        var px = (float)(cx + r * Math.Cos(angle));
        var py = (float)(cy + r * Math.Sin(angle));

        var hits = chart.GetPointsAt(new(px, py), FindingStrategy.CompareAll).ToArray();
        Assert.IsTrue(hits.Length >= 1, "Probe inside the first slice should hit at least one point");
    }

    [TestMethod]
    public void Pie_OutsideRadius_Misses()
    {
        var seriesCollection = new double[] { 100, 100, 100, 100 }.AsPieSeries();
        var chart = new SKPieChart
        {
            Width = 400,
            Height = 400,
            Series = seriesCollection,
        };
        _ = chart.GetImage();

        var hits = chart.GetPointsAt(new(395, 395), FindingStrategy.CompareAll).ToArray();
        Assert.AreEqual(0, hits.Length);
    }

    // --- RectangleHoverArea strategy contract ----------------------------
    // Pins that Automatic is rejected at the area level (the chart is
    // expected to resolve it via GetFindingStrategy() before calling).
    // Also pins the strict-edge contract — a probe exactly on X or Y edge
    // is a MISS (the ` > ` / ` < ` form in IsPointerOver).

    [TestMethod]
    public void RectangleHoverArea_Automatic_Throws()
    {
        var ha = new RectangleHoverArea().SetDimensions(0, 0, 10, 10);
        _ = Assert.ThrowsExactly<Exception>(
            () => ha.IsPointerOver(new LvcPoint(5, 5), FindingStrategy.Automatic));
    }

    [TestMethod]
    public void RectangleHoverArea_OnEdge_Misses()
    {
        var ha = new RectangleHoverArea().SetDimensions(10, 20, 30, 40);
        Assert.IsFalse(ha.IsPointerOver(new LvcPoint(10, 30), FindingStrategy.CompareAll), "left edge");
        Assert.IsFalse(ha.IsPointerOver(new LvcPoint(40, 30), FindingStrategy.CompareAll), "right edge");
        Assert.IsFalse(ha.IsPointerOver(new LvcPoint(20, 20), FindingStrategy.CompareAll), "top edge");
        Assert.IsFalse(ha.IsPointerOver(new LvcPoint(20, 60), FindingStrategy.CompareAll), "bottom edge");
        Assert.IsTrue(ha.IsPointerOver(new LvcPoint(20, 30), FindingStrategy.CompareAll), "interior");
    }

    // --- SemicircleHoverArea distance contract ---------------------------
    // Pins that DistanceTo measures from the slice midpoint anchored at
    // (CenterX, CenterY) — pre-fix it ignored center and computed against
    // a phantom point around the origin, breaking TakeClosest ordering.

    [TestMethod]
    public void SemicircleHoverArea_DistanceTo_AtSliceMidpoint_IsNearZero()
    {
        // First quadrant slice (0° to 90°), inner=0, outer=Radius/2.
        // Mid-radius = (0 + 100/2) / 2 = 25. Mid-angle = 45°.
        var ha = new SemicircleHoverArea
        {
            CenterX = 500,
            CenterY = 500,
            StartAngle = 0,
            EndAngle = 90,
            InnerRadius = 0,
            Radius = 100,
        };

        var rad = 45 * Math.PI / 180;
        var midX = (float)(500 + 25 * Math.Cos(rad));
        var midY = (float)(500 + 25 * Math.Sin(rad));

        var d = ha.DistanceTo(new LvcPoint(midX, midY), FindingStrategy.CompareAllTakeClosest);

        Assert.IsTrue(d < 0.001,
            $"DistanceTo must be ~0 at the slice midpoint; got {d}. " +
            "If this asserts again, DistanceTo dropped the CenterX/CenterY translation.");
    }

    [TestMethod]
    public void SemicircleHoverArea_DistanceTo_AtPieCenter_EqualsMidRadius()
    {
        // A probe at the pie center must be exactly mid-radius away from any
        // slice's midpoint (regardless of which slice — they all share the
        // same mid-radius for a given InnerRadius/Radius pair).
        var ha = new SemicircleHoverArea
        {
            CenterX = 500,
            CenterY = 500,
            StartAngle = 30,
            EndAngle = 70,
            InnerRadius = 20,
            Radius = 200,
        };

        var d = ha.DistanceTo(new LvcPoint(500, 500), FindingStrategy.CompareAllTakeClosest);

        var midRadius = (20 + 200 * 0.5f) * 0.5; // 60
        Assert.AreEqual(midRadius, d, 0.001);
    }

    // --- helpers ---------------------------------------------------------

    private static SKCartesianChart NewCartesianChart(ISeries series) =>
        new()
        {
            Width = 1000,
            Height = 1000,
            Series = [series],
            XAxes = [new Axis { IsVisible = false }],
            YAxes = [new Axis { IsVisible = false }],
        };

    private static RectangleHoverArea ReadHoverArea(ISeries series, SKCartesianChart chart, int index)
    {
        _ = chart.GetImage();
        var point = series.Fetch(chart.CoreChart).ElementAt(index);
        return (RectangleHoverArea)(point.Context.HoverArea
            ?? throw new InvalidOperationException("point has no HoverArea after Measure"));
    }

    private static TVisual ReadVisual<TVisual>(ISeries series, SKCartesianChart chart, int index)
        where TVisual : class
    {
        _ = chart.GetImage();
        var point = series.Fetch(chart.CoreChart).ElementAt(index);
        return (TVisual)point.Context.Visual!;
    }

    private static LvcPoint HoverAreaCenter(ISeries series, SKCartesianChart chart, int index)
    {
        var area = ReadHoverArea(series, chart, index);
        return new LvcPoint(area.X + area.Width * 0.5f, area.Y + area.Height * 0.5f);
    }
}
