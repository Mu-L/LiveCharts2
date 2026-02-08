using System;
using System.Linq;
using CoreTests.MockedObjects;
using LiveChartsCore.Drawing;
using LiveChartsCore.Drawing.Segments;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace CoreTests.SeriesTests;

[TestClass]
public class StackedAreaSeriesTest
{
    [TestMethod]
    public void ShouldScale()
    {
        var sutSeries = new StackedAreaSeries<double>
        {
            Values = [1, 2, 4, 8, 16, 32, 64, 128, 256],
            GeometrySize = 10
        };

        var sutSeries2 = new StackedAreaSeries<double>
        {
            Values = [1, 2, 4, 8, 16, 32, 64, 128, 256],
            GeometrySize = 10
        };

        var chart = new SKCartesianChart
        {
            Width = 1000,
            Height = 1000,
            Series = [sutSeries, sutSeries2],
            XAxes = [new Axis { MinLimit = -1, MaxLimit = 10 }],
            YAxes = [new Axis { MinLimit = 0, MaxLimit = 512 }]
        };

        _ = chart.GetImage();
        //chart.SaveImage("test.png"); // use this method to see the actual tested image

        var datafactory = sutSeries.DataFactory;
        var points = datafactory.Fetch(sutSeries, chart.CoreChart).ToArray();

        var unit = points.First(x => x.Coordinate.PrimaryValue == 1);
        var typedUnit = sutSeries.ConvertToTypedChartPoint(unit);

        var toCompareGuys = points.Where(x => x != unit).Select(sutSeries.ConvertToTypedChartPoint);

        var datafactory2 = sutSeries2.DataFactory;
        var points2 = datafactory2.Fetch(sutSeries2, chart.CoreChart).ToArray();
        var unit2 = points2.First(x => x.Coordinate.PrimaryValue == 1);
        var typedUnit2 = sutSeries.ConvertToTypedChartPoint(unit2);
        var toCompareGuys2 = points2.Where(x => x != unit2).Select(sutSeries2.ConvertToTypedChartPoint);

        // ensure the unit has valid dimensions
        Assert.IsTrue(typedUnit.Visual.Width == 10 && typedUnit.Visual.Height == 10);

        var previous = typedUnit;
        float? previousX = null;
        float? previousXArea = null;

        foreach (var sutPoint in toCompareGuys)
        {
            var previousBezier = ((CubicSegmentVisualPoint)previous.Context.AdditionalVisuals)?.Segment;
            var sutBezier = ((CubicSegmentVisualPoint)sutPoint.Context.AdditionalVisuals).Segment;

            // test x
            var currentDeltaX = previous.Visual.X - sutPoint.Visual.X;
            var currentDeltaAreaX = previousBezier.Xj - sutBezier.Xj;
            Assert.IsTrue(
                previousX is null
                ||
                Math.Abs(previousX.Value - currentDeltaX) < 0.001);
            Assert.IsTrue(
                previousXArea is null
                ||
                Math.Abs(previousXArea.Value - currentDeltaX) < 0.001);

            // test y
            var p = 1f - (sutPoint.Coordinate.PrimaryValue + sutPoint.StackedValue.Start) / 512f;
            Assert.IsTrue(
                Math.Abs(p * chart.CoreChart.DrawMarginSize.Height - sutPoint.Visual.Y + chart.CoreChart.DrawMarginLocation.Y) < 0.001);
            Assert.IsTrue(
                Math.Abs(p * chart.CoreChart.DrawMarginSize.Height - sutBezier.Yj + chart.CoreChart.DrawMarginLocation.Y) < 0.001);

            previousX = previous.Visual.X - sutPoint.Visual.X;
            previousXArea = previousBezier.Xj - sutBezier.Xj;
            previous = sutPoint;
        }

        previous = typedUnit2;
        previousX = null;
        previousXArea = null;
        foreach (var sutPoint in toCompareGuys2)
        {
            var previousBezier = ((CubicSegmentVisualPoint)previous.Context.AdditionalVisuals).Segment;
            var sutBezier = ((CubicSegmentVisualPoint)sutPoint.Context.AdditionalVisuals).Segment;

            // test x
            var currentDeltaX = previous.Visual.X - sutPoint.Visual.X;
            var currentDeltaAreaX = previousBezier.Xj - sutBezier.Xj;
            Assert.IsTrue(
                previousX is null
                ||
                Math.Abs(previousX.Value - currentDeltaX) < 0.001);
            Assert.IsTrue(
                previousXArea is null
                ||
                Math.Abs(previousXArea.Value - currentDeltaX) < 0.001);

            // test y
            var p = 1f - (sutPoint.Coordinate.PrimaryValue + sutPoint.StackedValue.Start) / 512f;
            Assert.IsTrue(
                Math.Abs(p * chart.CoreChart.DrawMarginSize.Height - sutPoint.Visual.Y + chart.CoreChart.DrawMarginLocation.Y) < 0.001);
            Assert.IsTrue(
                Math.Abs(p * chart.CoreChart.DrawMarginSize.Height - sutBezier.Yj + chart.CoreChart.DrawMarginLocation.Y) < 0.001);

            previousX = previous.Visual.X - sutPoint.Visual.X;
            previousXArea = previousBezier.Xj - sutBezier.Xj;
            previous = sutPoint;
        }
    }

    [TestMethod]
    public void ShouldPlaceDataLabel()
    {
        var gs = 5f;
        var sutSeries = new StackedAreaSeries<double, RectangleGeometry, TestLabel>
        {
            Values = [-10, -5, -1, 0, 1, 5, 10],
            DataPadding = new LvcPoint(0, 0),
            GeometrySize = gs * 2,
        };

        var chart = new SKCartesianChart
        {
            Width = 500,
            Height = 500,
            DrawMargin = new Margin(100),
            DrawMarginFrame = new DrawMarginFrame { Stroke = new SolidColorPaint(SKColors.Yellow, 2) },
            TooltipPosition = TooltipPosition.Top,
            Series = [sutSeries],
            XAxes = [new Axis { IsVisible = false }],
            YAxes = [new Axis { IsVisible = false }]
        };

        var datafactory = sutSeries.DataFactory;

        // TEST HIDDEN ===========================================================
        _ = chart.GetImage();

        var points = datafactory
            .Fetch(sutSeries, chart.CoreChart)
            .Select(sutSeries.ConvertToTypedChartPoint);

        Assert.IsTrue(sutSeries.DataLabelsPosition == DataLabelsPosition.End);
        Assert.IsTrue(points.All(x => x.Label is null));

        sutSeries.DataLabelsPaint = new SolidColorPaint
        {
            Color = SKColors.Black,
            SKTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        // TEST TOP ===============================================================
        sutSeries.DataLabelsPosition = DataLabelsPosition.Top;
        _ = chart.GetImage();

        points = datafactory
            .Fetch(sutSeries, chart.CoreChart)
            .Select(sutSeries.ConvertToTypedChartPoint);

        foreach (var p in points)
        {
            var v = p.Visual;
            var l = p.Label;

            l.Paint = sutSeries.DataLabelsPaint;
            var ls = l.Measure();

            Assert.IsTrue(
                Math.Abs(v.X + v.Width * 0.5f - l.X - gs) < 0.01 &&    // x is centered
                Math.Abs(v.Y - (l.Y + ls.Height * 0.5 + gs)) < 0.01);  // y is top
        }

        // TEST BOTTOM ===========================================================
        sutSeries.DataLabelsPosition = DataLabelsPosition.Bottom;

        _ = chart.GetImage();

        points = datafactory
            .Fetch(sutSeries, chart.CoreChart)
            .Select(sutSeries.ConvertToTypedChartPoint);

        foreach (var p in points)
        {
            var v = p.Visual;
            var l = p.Label;

            l.Paint = sutSeries.DataLabelsPaint;
            var ls = l.Measure();

            Assert.IsTrue(
                Math.Abs(v.X + v.Width * 0.5f - l.X - gs) < 0.01 &&              // x is centered
                Math.Abs(v.Y + v.Height - (l.Y - ls.Height * 0.5 + gs)) < 0.01); // y is bottom
        }

        // TEST RIGHT ============================================================
        sutSeries.DataLabelsPosition = DataLabelsPosition.Right;

        _ = chart.GetImage();

        points = datafactory
            .Fetch(sutSeries, chart.CoreChart)
            .Select(sutSeries.ConvertToTypedChartPoint);

        foreach (var p in points)
        {
            var v = p.Visual;
            var l = p.Label;

            l.Paint = sutSeries.DataLabelsPaint;
            var ls = l.Measure();

            Assert.IsTrue(
                Math.Abs(v.X + v.Width - (l.X - ls.Width * 0.5 + gs)) < 0.01 &&  // x is right
                Math.Abs(v.Y + v.Height * 0.5 - l.Y - gs) < 0.01);               // y is centered
        }

        // TEST LEFT =============================================================
        sutSeries.DataLabelsPosition = DataLabelsPosition.Left;

        _ = chart.GetImage();

        points = datafactory
            .Fetch(sutSeries, chart.CoreChart)
            .Select(sutSeries.ConvertToTypedChartPoint);

        foreach (var p in points)
        {
            var v = p.Visual;
            var l = p.Label;

            l.Paint = sutSeries.DataLabelsPaint;
            var ls = l.Measure();

            Assert.IsTrue(
                Math.Abs(v.X - (l.X + ls.Width * 0.5f + gs)) < 0.01 &&   // x is left
                Math.Abs(v.Y + v.Height * 0.5f - l.Y - gs) < 0.01);      // y is centered
        }

        // TEST MIDDLE ===========================================================
        sutSeries.DataLabelsPosition = DataLabelsPosition.Middle;

        _ = chart.GetImage();

        points = datafactory
            .Fetch(sutSeries, chart.CoreChart)
            .Select(sutSeries.ConvertToTypedChartPoint);

        foreach (var p in points)
        {
            var v = p.Visual;
            var l = p.Label;

            l.Paint = sutSeries.DataLabelsPaint;
            var ls = l.Measure();

            Assert.IsTrue(
                Math.Abs(v.X + v.Width * 0.5f - l.X - gs) < 0.01 &&      // x is centered
                Math.Abs(v.Y + v.Height * 0.5f - l.Y - gs) < 0.01);      // y is centered
        }

        // TEST START ===========================================================
        sutSeries.DataLabelsPosition = DataLabelsPosition.Start;

        _ = chart.GetImage();

        points = datafactory
            .Fetch(sutSeries, chart.CoreChart)
            .Select(sutSeries.ConvertToTypedChartPoint);

        foreach (var p in points)
        {
            var v = p.Visual;
            var l = p.Label;

            l.Paint = sutSeries.DataLabelsPaint;
            var ls = l.Measure();

            if (p.Model <= 0)
            {
                // it should be placed using the top position
                Assert.IsTrue(
                    Math.Abs(v.X + v.Width * 0.5f - l.X - gs) < 0.01 &&    // x is centered
                    Math.Abs(v.Y - (l.Y + ls.Height * 0.5 + gs)) < 0.01);  // y is top
            }
            else
            {
                // it should be placed using the bottom position
                Assert.IsTrue(
                    Math.Abs(v.X + v.Width * 0.5f - l.X - gs) < 0.01 &&              // x is centered
                    Math.Abs(v.Y + v.Height - (l.Y - ls.Height * 0.5 + gs)) < 0.01); // y is bottom
            }
        }

        // TEST END ===========================================================
        sutSeries.DataLabelsPosition = DataLabelsPosition.End;

        _ = chart.GetImage();

        points = datafactory
            .Fetch(sutSeries, chart.CoreChart)
            .Select(sutSeries.ConvertToTypedChartPoint);

        foreach (var p in points)
        {
            var v = p.Visual;
            var l = p.Label;

            l.Paint = sutSeries.DataLabelsPaint;
            var ls = l.Measure();

            if (p.Model <= 0)
            {
                // it should be placed using the bottom position
                Assert.IsTrue(
                    Math.Abs(v.X + v.Width * 0.5f - l.X - gs) < 0.01 &&              // x is centered
                    Math.Abs(v.Y + v.Height - (l.Y - ls.Height * 0.5 + gs)) < 0.01); // y is bottom
            }
            else
            {
                // it should be placed using the top position
                Assert.IsTrue(
                    Math.Abs(v.X + v.Width * 0.5f - l.X - gs) < 0.01 &&    // x is centered
                    Math.Abs(v.Y - (l.Y + ls.Height * 0.5 + gs)) < 0.01);  // y is top
            }
        }

        // FINALLY IF LABELS ARE NULL, IT SHOULD REMOVE THE CURRENT LABELS.
        var previousPaint = sutSeries.DataLabelsPaint;
        sutSeries.DataLabelsPaint = null;
        _ = chart.GetImage();

        Assert.IsTrue(!chart.CoreCanvas.ContainsPaintTask(previousPaint));
    }

    [TestMethod]
    public void ShouldHandleMixedPositiveNegativeValues()
    {
        // Regression test for #2086: Verify that stacked values are calculated correctly
        // when there are mixed positive and negative values across multiple series at the same index.
        // 
        // The fix ensures that both End and NegativeEnd are kept in sync to maintain proper
        // stacking relationships. This is necessary because:
        // - When Series2 starts, it needs to know where Series1 ended, regardless of whether
        //   Series1's last value was positive or negative
        // - Start is derived from the previous series' End (line 98 in Stacker.cs)
        // - NegativeStart is derived from the previous series' NegativeEnd (line 99 in Stacker.cs)
        // - For proper stacking, End and NegativeEnd must represent the same cumulative position
        
        // Series 1: positive at index 0, negative at index 1
        // Series 2: negative at index 0, positive at index 1
        var series1 = new StackedAreaSeries<double>
        {
            Values = [5, -3],
            GeometrySize = 10
        };

        var series2 = new StackedAreaSeries<double>
        {
            Values = [-2, 4],
            GeometrySize = 10
        };

        var chart = new SKCartesianChart
        {
            Width = 1000,
            Height = 1000,
            Series = [series1, series2],
            XAxes = [new Axis()],
            YAxes = [new Axis()]
        };

        _ = chart.GetImage();

        var datafactory1 = series1.DataFactory;
        var points1 = datafactory1.Fetch(series1, chart.CoreChart).ToArray();
        
        var datafactory2 = series2.DataFactory;
        var points2 = datafactory2.Fetch(series2, chart.CoreChart).ToArray();

        // At index 0: series1 = 5 (positive), series2 = -2 (negative)
        var point1_0 = points1[0];
        var point2_0 = points2[0];

        // At index 1: series1 = -3 (negative), series2 = 4 (positive)
        var point1_1 = points1[1];
        var point2_1 = points2[1];

        // Verify series1 at index 0 (positive value 5)
        // Both End and NegativeEnd are set to 5 to keep them in sync
        Assert.AreEqual(0, point1_0.StackedValue.Start, 0.001, "Series1[0] Start should be 0");
        Assert.AreEqual(5, point1_0.StackedValue.End, 0.001, "Series1[0] End should be 5");
        Assert.AreEqual(0, point1_0.StackedValue.NegativeStart, 0.001, "Series1[0] NegativeStart should be 0");
        Assert.AreEqual(5, point1_0.StackedValue.NegativeEnd, 0.001, "Series1[0] NegativeEnd should be 5 (kept in sync with End)");

        // Verify series2 at index 0 (negative value -2)
        // Start and NegativeStart are both derived from series1's End/NegativeEnd (both 5)
        // After adding -2, both End and NegativeEnd become 3
        Assert.AreEqual(5, point2_0.StackedValue.Start, 0.001, "Series2[0] Start should be 5 (from Series1 End)");
        Assert.AreEqual(3, point2_0.StackedValue.End, 0.001, "Series2[0] End should be 3 (5 + (-2))");
        Assert.AreEqual(5, point2_0.StackedValue.NegativeStart, 0.001, "Series2[0] NegativeStart should be 5 (from Series1 NegativeEnd)");
        Assert.AreEqual(3, point2_0.StackedValue.NegativeEnd, 0.001, "Series2[0] NegativeEnd should be 3 (kept in sync with End)");

        // Verify series1 at index 1 (negative value -3)
        // Both End and NegativeEnd are set to -3 to keep them in sync
        Assert.AreEqual(0, point1_1.StackedValue.Start, 0.001, "Series1[1] Start should be 0");
        Assert.AreEqual(-3, point1_1.StackedValue.End, 0.001, "Series1[1] End should be -3");
        Assert.AreEqual(0, point1_1.StackedValue.NegativeStart, 0.001, "Series1[1] NegativeStart should be 0");
        Assert.AreEqual(-3, point1_1.StackedValue.NegativeEnd, 0.001, "Series1[1] NegativeEnd should be -3 (kept in sync with End)");

        // Verify series2 at index 1 (positive value 4)
        // Start and NegativeStart are both derived from series1's End/NegativeEnd (both -3)
        // After adding 4, both End and NegativeEnd become 1
        Assert.AreEqual(-3, point2_1.StackedValue.Start, 0.001, "Series2[1] Start should be -3 (from Series1 End)");
        Assert.AreEqual(1, point2_1.StackedValue.End, 0.001, "Series2[1] End should be 1 (-3 + 4)");
        Assert.AreEqual(-3, point2_1.StackedValue.NegativeStart, 0.001, "Series2[1] NegativeStart should be -3 (from Series1 NegativeEnd)");
        Assert.AreEqual(1, point2_1.StackedValue.NegativeEnd, 0.001, "Series2[1] NegativeEnd should be 1 (kept in sync with End)");
    }
}
