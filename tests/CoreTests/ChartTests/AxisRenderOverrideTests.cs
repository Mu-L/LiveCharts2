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

    private sealed class FakeEngine(IAxisRenderOverride grouper) : SkiaSharpProvider
    {
        public override IAxisRenderOverride? GetAxisRenderOverride(ICartesianAxis axis) => grouper;
    }

    // Yields its items once, then throws if enumerated a second time — proves CoreAxis materializes the
    // override's separators rather than re-enumerating the same iterator in the size and draw passes.
    private sealed class OnceEnumerable(double[] items) : IEnumerable<double>
    {
        private int _calls;
        public IEnumerator<double> GetEnumerator() => _calls++ == 0
            ? ((IEnumerable<double>)items).GetEnumerator()
            : throw new InvalidOperationException("single-use iterator enumerated twice");
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class SingleUseGrouper : IAxisRenderOverride
    {
        public bool LabelerUsed;
        public bool TryGroup(
            ICartesianAxis axis, Chart chart, double min, double max,
            out IEnumerable<double>? separators, out Func<double, string>? labeler)
        {
            separators = new OnceEnumerable([10d, 50d, 90d]);
            labeler = _ => { LabelerUsed = true; return "X"; };
            return true;
        }
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
    public void GroupedSeparators_SingleUseIterator_AreMaterialized()
    {
        var grouper = new SingleUseGrouper();
        try
        {
            LiveCharts.Configure(s => s.HasProvider(new FakeEngine(grouper)));

            // Re-enumerating a single-use iterator (size pass + draw pass) would throw; materializing
            // it once must not. The labeler running proves the separators reached the draw pass.
            _ = new SKCartesianChart
            {
                Width = 600,
                Height = 400,
                Series = [new LineSeries<double> { Values = [0, 100], GeometrySize = 0 }],
                XAxes = [new Axis { GroupDates = true, MinLimit = 0, MaxLimit = 100 }],
                YAxes = [new Axis()],
            }.GetImage();

            Assert.IsTrue(grouper.LabelerUsed, "the grouped labeler must run, so the separators were used in the draw pass");
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
