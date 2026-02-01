using LiveChartsCore.SkiaSharpView.SKCharts;
using LiveChartsCore.SkiaSharpView.VisualElements;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests.ChartTests;

[TestClass]
public class ChartTests
{
    // based on https://github.com/beto-rodriguez/LiveCharts2/issues/1422

    // in theory LiveCharts properties should never be null, but we can't control the user input
    // specially on this case where DataContext could be null

    [TestMethod]
    public void CartesianShouldHandleNullParams()
    {
        // we are testing the properties defined on LiveChartsCore/Kernel/Sketches/ICartesianChartView.cs

        var chart = new SKCartesianChart
        {
            Width = 1000,
            Height = 1000,
            XAxes = null,
            YAxes = null,
            Sections = null,
            Series = null,
            DrawMarginFrame = null,
            VisualElements = null
        };

        var image = chart.GetImage();

        Assert.IsTrue(image is not null);
    }

    [TestMethod]
    public void PieShouldHandleNullParams()
    {
        // we are testing the properties defined on LiveChartsCore/Kernel/Sketches/IPieChartView.cs

        var chart = new SKPieChart
        {
            Width = 1000,
            Height = 1000,
            Series = null,
            VisualElements = null
        };

        var image = chart.GetImage();

        Assert.IsTrue(image is not null);
    }

    [TestMethod]
    public void PieShouldResizeAngularTicks()
    {
        // https://github.com/Live-Charts/LiveCharts2/issues/2012

        var needle = new NeedleVisual { Value = 45 };
        var ticks = new AngularTicksVisual
        {
            Labeler = value => value.ToString("N1"),
            LabelsSize = 16,
            LabelsOuterOffset = 15,
            OuterOffset = 65,
            TicksLength = 20
        };

        var chart = new SKPieChart
        {
            Width = 1000,
            Height = 1000,
            MaxAngle = 270,
            MinValue = 0,
            MaxValue = 100,
            Series = [],
            VisualElements = [needle, ticks]
        };

        _ = chart.GetImage();
        chart.MaxValue = 10;
        _ = chart.GetImage();
    }

    [TestMethod]
    public void PolarShouldHandleNullParams()
    {
        // we are testing the properties defined on LiveChartsCore/Kernel/Sketches/IPolarChartView.cs

        var chart = new SKPolarChart
        {
            Width = 1000,
            Height = 1000,
            Series = null,
            AngleAxes = null,
            RadiusAxes = null,
            VisualElements = null
        };

        var image = chart.GetImage();

        Assert.IsTrue(image is not null);
    }

    [TestMethod]
    public void MapShouldHandleNullParams()
    {
        // we are testing the properties defined on LiveChartsCore/Kernel/Sketches/IGeoMapView.cs

        var chart = new SKGeoMap
        {
            Width = 1000,
            Height = 1000,
            Series = null,
        };

        var image = chart.GetImage();

        Assert.IsTrue(image is not null);
    }
}
