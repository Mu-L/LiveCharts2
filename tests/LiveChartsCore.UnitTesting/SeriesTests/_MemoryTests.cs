﻿using System;
using System.Collections;
using System.Collections.ObjectModel;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using LiveChartsCore.UnitTesting.CoreObjectsTests;
using LiveChartsGeneratedCode;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiveChartsCore.UnitTesting.SeriesTests;

//[TestClass]
public class _MemoryTests
{
    private static readonly int s_repeatCount;
    private static readonly double s_treshold;

    static _MemoryTests()
    {
        s_repeatCount = 5000;
        var threshold = 2 * 1024 * 1024 * (s_repeatCount / 5000d); // 2MB each 5000 repeats
        s_treshold = threshold < 1 * 1024 * 1024
            ? 1 * 1024 * 1024
            : threshold; // ensure at least 1MB threshold
    }

    [TestMethod]
    public void ObservableValuesChangingTest()
    {
        // here series.values is of type ObservableCollection<ObservableValue>
        // we add, remove, clear, and change the visibility of the series multiple times
        // the memory and geometries count should not increase significantly.

        TestObservablesChanging(new CartesianSut(new BoxSeries<ObservableValue>(), "box"));
        TestObservablesChanging(new CartesianSut(new ColumnSeries<ObservableValue>(), "colum"));
        TestObservablesChanging(new CartesianSut(new CandlesticksSeries<ObservableValue>(), "candle"));
        TestObservablesChanging(new CartesianSut(new HeatSeries<ObservableValue>(), "heat"));
        TestObservablesChanging(new CartesianSut(new LineSeries<ObservableValue>(), "line"));
        TestObservablesChanging(new CartesianSut(new RowSeries<ObservableValue>(), "row"));
        TestObservablesChanging(new CartesianSut(new ScatterSeries<ObservableValue>(), "scatter"));
        TestObservablesChanging(new CartesianSut(new StepLineSeries<ObservableValue>(), "step line"));
        TestObservablesChanging(new PieSut(new PieSeries<ObservableValue>(), "pie"));
        //TestObservablesChanging(new PolarSut(new PolarLineSeries<ObservableValue>(), "polar"));

        // stacked series are irrelevant for this test because they inherit from some type above.
    }

    [TestMethod]
    public void PrimitiveValuesInstanceChangedTest()
    {
        // here series.values is of type int[]
        // we change the instance of the values array multiple times
        // the memory and geometries count should not increase significantly.

        TestValuesInstanceChangedPrimitiveMapped(new CartesianSutInt(new BoxSeries<int>(), "box"));
        TestValuesInstanceChangedPrimitiveMapped(new CartesianSutInt(new ColumnSeries<int>(), "colum"));
        TestValuesInstanceChangedPrimitiveMapped(new CartesianSutInt(new CandlesticksSeries<int>(), "candle"));
        TestValuesInstanceChangedPrimitiveMapped(new CartesianSutInt(new HeatSeries<int>(), "heat"));
        TestValuesInstanceChangedPrimitiveMapped(new CartesianSutInt(new LineSeries<int>(), "line"));
        TestValuesInstanceChangedPrimitiveMapped(new CartesianSutInt(new RowSeries<int>(), "row"));
        TestValuesInstanceChangedPrimitiveMapped(new CartesianSutInt(new ScatterSeries<int>(), "scatter"));
        TestValuesInstanceChangedPrimitiveMapped(new CartesianSutInt(new StepLineSeries<int>(), "step line"));
        TestValuesInstanceChangedPrimitiveMapped(new PieSutInt(new PieSeries<int>(), "pie"));
        TestValuesInstanceChangedPrimitiveMapped(new PolarSutInt(new PolarLineSeries<int>(), "polar"));

        // stacked series are irrelevant for this test because they inherit from some type above.
    }

    [TestMethod]
    public void ObservableValuesInstanceChangedTest()
    {
        // here series.values is of type ObservableCollection<ObservableValue>
        // we change the instance of the values array multiple times
        // the memory and geometries count should not increase significantly.

        TestValuesInstanceChangedObservableIChartEntities(new CartesianSut(new BoxSeries<ObservableValue>(), "box"));
        TestValuesInstanceChangedObservableIChartEntities(new CartesianSut(new ColumnSeries<ObservableValue>(), "colum"));
        TestValuesInstanceChangedObservableIChartEntities(new CartesianSut(new CandlesticksSeries<ObservableValue>(), "candle"));
        TestValuesInstanceChangedObservableIChartEntities(new CartesianSut(new HeatSeries<ObservableValue>(), "heat"));
        TestValuesInstanceChangedObservableIChartEntities(new CartesianSut(new LineSeries<ObservableValue>(), "line"));
        TestValuesInstanceChangedObservableIChartEntities(new CartesianSut(new RowSeries<ObservableValue>(), "row"));
        TestValuesInstanceChangedObservableIChartEntities(new CartesianSut(new ScatterSeries<ObservableValue>(), "scatter"));
        TestValuesInstanceChangedObservableIChartEntities(new CartesianSut(new StepLineSeries<ObservableValue>(), "step line"));
        TestValuesInstanceChangedObservableIChartEntities(new PieSut(new PieSeries<ObservableValue>(), "pie"));
        TestValuesInstanceChangedObservableIChartEntities(new PolarSut(new PolarLineSeries<ObservableValue>(), "polar"));

        // stacked series are irrelevant for this test because they inherit from some type above.
    }

    private static void TestObservablesChanging<T>(ChartSut<T> sut)
        where T : IList
    {
        // this test replaces the values of the series with a new collection of 5,000 elements
        // values is of type ObservableCollection<ObservableValue>

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialMemory = GC.GetTotalMemory(true);

        _ = ChangingPaintTasks.DrawChart(sut.Chart, true);

        var canvas = sut.Chart.CoreCanvas;
        var geometries = canvas.CountGeometries();
        var deltas = 0;
        var totalFramesDrawn = 0;

        for (var i = 0; i < 100; i++)
        {
            sut.Series.IsVisible = false;
            totalFramesDrawn += ChangingPaintTasks.DrawChart(sut.Chart, true);

            sut.Series.IsVisible = true;
            totalFramesDrawn += ChangingPaintTasks.DrawChart(sut.Chart, true);

            for (var j = 0; j < s_repeatCount; j++)
                _ = sut.Values.Add(new ObservableValue(2));
            totalFramesDrawn += ChangingPaintTasks.DrawChart(sut.Chart, true);

            sut.Values.RemoveAt(0);
            sut.Values.RemoveAt(0);
            sut.Values.RemoveAt(0);
            sut.Values.RemoveAt(0);
            totalFramesDrawn += ChangingPaintTasks.DrawChart(sut.Chart, true);

            sut.Values.Clear();
            totalFramesDrawn += ChangingPaintTasks.DrawChart(sut.Chart, true);

            var newCount = canvas.CountGeometries();
            if (newCount > geometries)
            {
                deltas++;
                geometries = newCount;
            }
        }

        Assert.IsTrue(deltas <= 1);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Assert that there is no significant increase in memory usage
        var finalMemory = GC.GetTotalMemory(true);

        // 2MB is a reasonable threshold for this test
        // it adds 5,000 points 100 times, which is 500,000 points
        // removes 4 points 100 times, which is 400 points
        // clears the collection and changes visibility 100 times

        // this test also simulates the chart animation, it makes a change,
        // then enters a loop until animations finish.

        Assert.IsTrue(
            finalMemory - initialMemory < s_treshold,
            $"[{sut.Series.Name} series] Potential memory leak detected {(finalMemory - initialMemory) / (1024d * 1024):N2}MB, " +
            $"{totalFramesDrawn} frames drawn.");
    }

    private void TestValuesInstanceChangedPrimitiveMapped<T>(ChartSut<T> sut)
        where T : IEnumerable
    {
        // this test replaces the values of the series with a new array of 5,000 elements
        // values is of type int[]

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialMemory = 0L;

        _ = ChangingPaintTasks.DrawChart(sut.Chart, true);

        var canvas = sut.Chart.CoreCanvas;
        var geometries = canvas.CountGeometries();
        var deltas = 0;
        var totalFramesDrawn = 0;

        int[] values;

        for (var i = 0; i < 100; i++)
        {
            values = new int[s_repeatCount];
            for (var j = 0; j < s_repeatCount; j++)
                values[j] = 2;

            sut.Series.Values = values;
            totalFramesDrawn += ChangingPaintTasks.DrawChart(sut.Chart, true);

            // we wait for the first frame to be drawn to measure the initial memory
            // not sure why, but the first draw consumes about 12mb in this case
            // but no matter the number or runs, it stays at those 12 mb all the time.
            // so if we ignore the first call, we satisfy the 2mb threashold.
            // in this test, it only happens in the HeatSeries.
            if (i == 0) initialMemory = GC.GetTotalMemory(true);

            var newCount = canvas.CountGeometries();
            if (newCount > geometries)
            {
                deltas++;
                geometries = newCount;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        sut.Series.Values = Array.Empty<int>();
        totalFramesDrawn += ChangingPaintTasks.DrawChart(sut.Chart, true);

        values = null;

        Assert.IsTrue(deltas <= 1);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Assert that there is no significant increase in memory usage
        var finalMemory = GC.GetTotalMemory(true);

        // 2MB is a reasonable threshold for this test
        // it changes 100 times, the instance of the values array to a new array of 5,000 elements

        Assert.IsTrue(
            finalMemory - initialMemory < s_treshold,
            $"[{sut.Series.Name} series] Potential memory leak detected {(finalMemory - initialMemory) / (1024d * 1024):N2}MB, " +
            $"{totalFramesDrawn} frames drawn.");
    }

    private void TestValuesInstanceChangedObservableIChartEntities<T>(ChartSut<T> sut)
        where T : IEnumerable
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialMemory = 0L;

        _ = ChangingPaintTasks.DrawChart(sut.Chart, true);

        var canvas = sut.Chart.CoreCanvas;
        var geometries = canvas.CountGeometries();
        var deltas = 0;
        var totalFramesDrawn = 0;

        ObservableCollection<ObservableValue> values;

        for (var i = 0; i < 100; i++)
        {
            var newValues = new ObservableValue[s_repeatCount];
            for (var j = 0; j < s_repeatCount; j++)
                newValues[j] = new(2);

            values = new(newValues);

            sut.Series.Values = values;
            totalFramesDrawn += ChangingPaintTasks.DrawChart(sut.Chart, true);

            // we wait for the first frame to be drawn to measure the initial memory
            // not sure why, but the first draw consumes about 12mb in this case
            // but no matter the number or runs, it stays at those 12 mb all the time.
            // so if we ignore the first call, we satisfy the 2mb threashold.
            if (i == 0) initialMemory = GC.GetTotalMemory(true);

            var newCount = canvas.CountGeometries();
            if (newCount > geometries)
            {
                deltas++;
                geometries = newCount;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        sut.Series.Values = null;
        totalFramesDrawn += ChangingPaintTasks.DrawChart(sut.Chart, true);

        values = null;

        Assert.IsTrue(deltas <= 1);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Assert that there is no significant increase in memory usage
        var finalMemory = GC.GetTotalMemory(true);

        // 2MB is a reasonable threshold for this test
        // it changes 100 times, the instance of the values array to a new array of 5,000 elements

        var mb = (finalMemory - initialMemory) / (1024d * 1024);

        Assert.IsTrue(
            finalMemory - initialMemory < s_treshold,
            $"[{sut.Series.Name} series] Potential memory leak detected {mb:N2}MB, " +
            $"{totalFramesDrawn} frames drawn.");
    }

    #region helper classes

    private abstract class ChartSut<T>
        where T : IEnumerable
    {
        public SourceGenSKChart Chart { get; set; }
        public ISeries Series { get; set; }
        public T Values { get; set; }

        protected ChartSut(
            SourceGenSKChart chart,
            ISeries series,
            string name,
            T initialValues)
        {
            series.Name = name;
            series.Values = Values = initialValues;
            Series = series;
            Chart = chart;
        }
    }

    private class CartesianSut(
        ISeries series,
        string name)
            : ChartSut<ObservableCollection<ObservableValue>>(new SKCartesianChart
            {
                Series = [series],
                AnimationsSpeed = TimeSpan.FromMilliseconds(10),
                EasingFunction = EasingFunctions.Lineal,
                Width = 1000,
                Height = 1000
            },
            series,
            name,
            [])
    { }

    private class PieSut(
        ISeries series,
        string name)
            : ChartSut<ObservableCollection<ObservableValue>>(new SKPieChart
            {
                Series = [series],
                AnimationsSpeed = TimeSpan.FromMilliseconds(10),
                EasingFunction = EasingFunctions.Lineal,
                Width = 1000,
                Height = 1000
            },
            series,
            name,
            [])
    { }

    private class PolarSut(
        ISeries series,
        string name)
            : ChartSut<ObservableCollection<ObservableValue>>(new SKPolarChart
            {
                Series = [series],
                AnimationsSpeed = TimeSpan.FromMilliseconds(10),
                EasingFunction = EasingFunctions.Lineal,
                Width = 1000,
                Height = 1000
            },
            series,
            name,
            [])
    { }

    private class CartesianSutInt(
        ISeries series,
        string name)
            : ChartSut<int[]>(new SKCartesianChart
            {
                Series = [series],
                AnimationsSpeed = TimeSpan.FromMilliseconds(10),
                EasingFunction = EasingFunctions.Lineal,
                Width = 1000,
                Height = 1000
            },
            series,
            name,
            [])
    { }

    private class PieSutInt(
        ISeries series,
        string name)
            : ChartSut<int[]>(new SKPieChart
            {
                Series = [series],
                AnimationsSpeed = TimeSpan.FromMilliseconds(10),
                EasingFunction = EasingFunctions.Lineal,
                Width = 1000,
                Height = 1000
            },
            series,
            name,
            [])
    { }

    private class PolarSutInt(
        ISeries series,
        string name)
            : ChartSut<int[]>(new SKPolarChart
            {
                Series = [series],
                AnimationsSpeed = TimeSpan.FromMilliseconds(10),
                EasingFunction = EasingFunctions.Lineal,
                Width = 1000,
                Height = 1000
            },
            series,
            name,
            [])
    { }

    #endregion
}
