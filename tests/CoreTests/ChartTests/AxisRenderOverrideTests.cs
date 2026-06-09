using System;
using System.Collections.Generic;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Providers;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests.ChartTests;

// Guards the IAxisRenderOverride provider seam: when an axis sets GroupDates, the provider's override
// is consulted on measure and the separators/labeler it returns are used; when GroupDates is false the
// override is never consulted and the axis lays itself out normally.
[TestClass]
public class AxisRenderOverrideTests
{
    private sealed class RecordingGrouper : IAxisRenderOverride
    {
        public bool Consulted, LabelerUsed;
        public double SeenMin, SeenMax;

        public bool TryGroup(
            ICartesianAxis axis, Chart chart, double min, double max,
            out IEnumerable<double>? separators, out Func<double, string>? labeler)
        {
            Consulted = true;
            SeenMin = min;
            SeenMax = max;
            separators = [10d, 50d, 90d];                 // a fixed, small set unlike the default
            labeler = _ => { LabelerUsed = true; return "X"; };
            return true;
        }
    }

    private sealed class FakeEngine(RecordingGrouper grouper) : SkiaSharpProvider
    {
        public override IAxisRenderOverride? GetAxisRenderOverride(ICartesianAxis axis) => grouper;
    }

    private static SKCartesianChart NewChart(bool groupDates) => new()
    {
        Width = 600,
        Height = 400,
        Series = [new LineSeries<double> { Values = [0, 100], GeometrySize = 0 }],
        XAxes = [new Axis { GroupDates = groupDates, MinLimit = 0, MaxLimit = 100 }],
        YAxes = [new Axis()],
    };

    [TestMethod]
    public void GroupDates_True_ConsultsOverride_AndUsesItsLabeler()
    {
        var grouper = new RecordingGrouper();
        try
        {
            LiveCharts.Configure(s => s.HasProvider(new FakeEngine(grouper)));
            _ = NewChart(groupDates: true).GetImage();

            Assert.IsTrue(grouper.Consulted, "the override must be consulted when GroupDates is true");
            Assert.IsTrue(grouper.LabelerUsed, "the override's labeler must be used to draw the labels");
            Assert.AreEqual(0d, grouper.SeenMin, 1e-6, "the override must receive the visible min");
            Assert.AreEqual(100d, grouper.SeenMax, 1e-6, "the override must receive the visible max");
        }
        finally
        {
            LiveCharts.Configure(s => s.AddSkiaSharp());
        }
    }

    [TestMethod]
    public void GroupDates_False_DoesNotConsultOverride()
    {
        var grouper = new RecordingGrouper();
        try
        {
            LiveCharts.Configure(s => s.HasProvider(new FakeEngine(grouper)));
            _ = NewChart(groupDates: false).GetImage();

            Assert.IsFalse(grouper.Consulted, "the override must NOT be consulted when GroupDates is false");
        }
        finally
        {
            LiveCharts.Configure(s => s.AddSkiaSharp());
        }
    }
}
