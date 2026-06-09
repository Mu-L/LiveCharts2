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

// Guards the IAxisRenderOverride provider seam: the engine decides which axes are overridden (here,
// like the real engines, by the concrete axis type plus its GroupTimeUnits flag); when the engine
// returns an override it is consulted on measure and the separators/labeler it returns are used; when
// the engine returns null the override is never consulted and the axis lays itself out normally.
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

    // Mirrors how a real engine is expected to gate the seam: by the concrete axis type and its own
    // opt-in flag — core no longer carries any grouping flag.
    private sealed class FakeEngine(IAxisRenderOverride grouper) : SkiaSharpProvider
    {
        public override IAxisRenderOverride? GetAxisRenderOverride(ICartesianAxis axis) =>
            axis switch
            {
                DateTimeAxis { GroupTimeUnits: true } => grouper,
                TimeSpanAxis { GroupTimeUnits: true } => grouper,
                _ => null
            };
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

    private static SKCartesianChart NewChart(ICartesianAxis xAxis) => new()
    {
        Width = 600,
        Height = 400,
        Series = [new LineSeries<double> { Values = [0, 100], GeometrySize = 0 }],
        XAxes = [xAxis],
        YAxes = [new Axis()],
    };

    private static DateTimeAxis NewDateTimeAxis(bool groupTimeUnits) =>
        new(TimeSpan.FromTicks(1), date => date.Ticks.ToString())
        {
            GroupTimeUnits = groupTimeUnits,
            MinLimit = 0,
            MaxLimit = 100
        };

    [TestMethod]
    public void GroupTimeUnits_True_ConsultsOverride_AndUsesItsLabeler()
    {
        var grouper = new RecordingGrouper();
        try
        {
            LiveCharts.Configure(s => s.HasProvider(new FakeEngine(grouper)));
            _ = NewChart(NewDateTimeAxis(groupTimeUnits: true)).GetImage();

            Assert.IsTrue(grouper.Consulted, "the override must be consulted when GroupTimeUnits is true");
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
            _ = NewChart(NewDateTimeAxis(groupTimeUnits: true)).GetImage();

            Assert.IsTrue(grouper.LabelerUsed, "the grouped labeler must run, so the separators were used in the draw pass");
        }
        finally
        {
            LiveCharts.Configure(s => s.AddSkiaSharp());
        }
    }

    [TestMethod]
    public void GroupTimeUnits_False_DoesNotConsultOverride()
    {
        var grouper = new RecordingGrouper();
        try
        {
            LiveCharts.Configure(s => s.HasProvider(new FakeEngine(grouper)));
            _ = NewChart(NewDateTimeAxis(groupTimeUnits: false)).GetImage();

            Assert.IsFalse(grouper.Consulted, "the override must NOT be consulted when GroupTimeUnits is false");
        }
        finally
        {
            LiveCharts.Configure(s => s.AddSkiaSharp());
        }
    }
}
