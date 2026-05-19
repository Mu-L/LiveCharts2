using LiveChartsCore.Drawing;
using LiveChartsCore.Geo;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests.CoreObjectsTests;

[TestClass]
public class GeoMapEventsTests
{
    [TestMethod]
    public void LandClickedFiresWhenClickingInsideALand()
    {
        var chart = NewGeoMap();
        chart.CoreChart.Measure();

        LandClickedEventArgs? args = null;
        chart.CoreChart.LandClicked += a => args = a;

        var moscow = MoscowPixel(chart);
        chart.CoreChart.InvokePointerDown(moscow, isSecondaryAction: false);
        chart.CoreChart.InvokePointerUp(moscow, isSecondaryAction: false);

        Assert.IsNotNull(args);
        Assert.AreEqual("rus", args!.Land.ShortName);
    }

    [TestMethod]
    public void LandClickedDoesNotFireWhenClickingTheOcean()
    {
        var chart = NewGeoMap();
        chart.CoreChart.Measure();

        var fired = false;
        chart.CoreChart.LandClicked += _ => fired = true;

        // Mid-Atlantic: lon=-30, lat=0 sits in open ocean on Mercator.
        var projector = Maps.BuildProjector(MapProjection.Mercator, [600f, 600f]);
        projector.ToMap(-30, 0, out var oceanX, out var oceanY);
        var ocean = new LvcPoint(oceanX, oceanY);
        chart.CoreChart.InvokePointerDown(ocean, isSecondaryAction: false);
        chart.CoreChart.InvokePointerUp(ocean, isSecondaryAction: false);

        Assert.IsFalse(fired);
    }

    [TestMethod]
    public void LandClickedArgsCarryHeatValueAndPosition()
    {
        var chart = NewGeoMap();
        chart.CoreChart.Measure();

        LandClickedEventArgs? args = null;
        chart.CoreChart.LandClicked += a => args = a;

        var moscow = MoscowPixel(chart);
        chart.CoreChart.InvokePointerDown(moscow, isSecondaryAction: false);
        chart.CoreChart.InvokePointerUp(moscow, isSecondaryAction: false);

        Assert.IsNotNull(args);
        Assert.AreEqual(42d, args!.Value);
        Assert.AreEqual(moscow.X, args.Position.X);
        Assert.AreEqual(moscow.Y, args.Position.Y);
    }

    [TestMethod]
    public void LandClickedDoesNotFireWhenPointerMovesPastClickThreshold()
    {
        // GeoMapChart tracks a 5px click-vs-drag threshold in InvokePointerMove;
        // a drag that exceeds it flips _pointerDownIsClick=false and the
        // subsequent pointer-up must NOT fire LandClicked.
        var chart = NewGeoMap();
        chart.CoreChart.Measure();

        var fired = false;
        chart.CoreChart.LandClicked += _ => fired = true;

        var moscow = MoscowPixel(chart);
        chart.CoreChart.InvokePointerDown(moscow, isSecondaryAction: false);
        chart.CoreChart.InvokePointerMove(new LvcPoint(moscow.X + 20, moscow.Y + 20));
        chart.CoreChart.InvokePointerUp(new LvcPoint(moscow.X + 20, moscow.Y + 20), isSecondaryAction: false);

        Assert.IsFalse(fired);
    }

    private static SKGeoMap NewGeoMap() => new()
    {
        Width = 600,
        Height = 600,
        MapProjection = MapProjection.Mercator,
        Series = [new HeatLandSeries { Lands = [new() { Name = "rus", Value = 42 }] }],
    };

    private static LvcPoint MoscowPixel(SKGeoMap chart)
    {
        var projector = Maps.BuildProjector(
            MapProjection.Mercator,
            [chart.Width, chart.Height]);
        projector.ToMap(37.62, 55.75, out var x, out var y);
        return new LvcPoint(x, y);
    }
}
