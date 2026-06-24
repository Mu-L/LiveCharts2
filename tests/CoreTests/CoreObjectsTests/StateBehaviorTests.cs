using System.Collections.Generic;
using LiveChartsCore.Drawing;
using LiveChartsCore.Painting;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.VisualStates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static LiveChartsCore.VisualStates.VisualStatesDictionary;

namespace CoreTests.CoreObjectsTests;

// Visual states can carry an imperative IStateBehavior alongside their value setters. These tests
// pin the contract: a behavior is applied once when its state becomes active and reversed once when
// the state is removed, it never fires for a state that was not active, and it coexists with value
// setters (which keep their own save/restore). A SolidColorPaint is used as a convenient Animatable
// target since it exposes registered motion properties (e.g. StrokeThickness).
[TestClass]
public class StateBehaviorTests
{
    private sealed class RecordingBehavior : IStateBehavior
    {
        public int Applied { get; private set; }
        public int Removed { get; private set; }
        public Animatable? LastTarget { get; private set; }

        public void OnStateApplied(Animatable target) { Applied++; LastTarget = target; }
        public void OnStateRemoved(Animatable target) { Removed++; LastTarget = target; }
    }

    private static VisualStatesDictionary StatesWith(
        string name, IStateBehavior behavior, params (string, object)[] setters)
    {
        var setterMap = new Dictionary<string, DrawnPropertySetter>();
        foreach (var (property, value) in setters)
            setterMap[property] = new DrawnPropertySetter(property, value);

        return new VisualStatesDictionary
        {
            [name] = new DrawnPropertiesDictionary(setterMap, isInternalSet: false, behaviors: [behavior])
        };
    }

    [TestMethod]
    public void Behavior_FiresOnceOnApply_AndOnceOnRemove()
    {
        var behavior = new RecordingBehavior();
        var states = StatesWith("Hover", behavior);
        var target = new SolidColorPaint();

        states.SetState("Hover", target);
        states.SetState("Hover", target); // re-applying an already-active state must not re-fire

        Assert.AreEqual(1, behavior.Applied);
        Assert.AreEqual(0, behavior.Removed);
        Assert.AreSame(target, behavior.LastTarget);

        states.ClearState("Hover", target);
        states.ClearState("Hover", target); // clearing an inactive state must not re-fire

        Assert.AreEqual(1, behavior.Applied);
        Assert.AreEqual(1, behavior.Removed);
    }

    [TestMethod]
    public void Behavior_NotRemoved_WhenStateNeverApplied()
    {
        var behavior = new RecordingBehavior();
        var states = StatesWith("Hover", behavior);
        var target = new SolidColorPaint();

        states.ClearState("Hover", target);

        Assert.AreEqual(0, behavior.Applied);
        Assert.AreEqual(0, behavior.Removed);
    }

    [TestMethod]
    public void StateBehavior_LambdaAdapter_InvokesDelegates()
    {
        var applied = 0;
        var removed = 0;
        Animatable? appliedTarget = null;

        var behavior = new StateBehavior(
            onStateApplied: a => { applied++; appliedTarget = a; },
            onStateRemoved: _ => removed++);

        var states = StatesWith("Hover", behavior);
        var target = new SolidColorPaint();

        states.SetState("Hover", target);
        Assert.AreEqual(1, applied);
        Assert.AreSame(target, appliedTarget);

        states.ClearState("Hover", target);
        Assert.AreEqual(1, removed);
    }

    [TestMethod]
    public void StateBehavior_NullDelegates_AreNoOps()
    {
        var states = StatesWith("Hover", new StateBehavior());
        var target = new SolidColorPaint();

        states.SetState("Hover", target);
        states.ClearState("Hover", target); // must not throw
    }

    [TestMethod]
    public void Behavior_RunsAlongsideValueSetters()
    {
        var behavior = new RecordingBehavior();
        var target = new SolidColorPaint { StrokeThickness = 2f };
        var states = StatesWith("Hover", behavior, (nameof(Paint.StrokeThickness), 10f));

        states.SetState("Hover", target);
        Assert.AreEqual(1, behavior.Applied);
        Assert.AreEqual(10f, ThicknessTarget(target), 0.001f); // value setter applied

        states.ClearState("Hover", target);
        Assert.AreEqual(1, behavior.Removed);
        Assert.AreEqual(2f, ThicknessTarget(target), 0.001f); // value setter restored to the original
    }

    [TestMethod]
    public void ClearStates_ReversesEveryActiveBehavior()
    {
        var hover = new RecordingBehavior();
        var selected = new RecordingBehavior();
        var target = new SolidColorPaint();

        var states = new VisualStatesDictionary
        {
            ["Hover"] = new DrawnPropertiesDictionary(
                new Dictionary<string, DrawnPropertySetter>(), isInternalSet: false, behaviors: [hover]),
            ["Selected"] = new DrawnPropertiesDictionary(
                new Dictionary<string, DrawnPropertySetter>(), isInternalSet: false, behaviors: [selected])
        };

        states.SetState("Hover", target);
        states.SetState("Selected", target);
        states.ClearStates(target);

        Assert.AreEqual(1, hover.Applied);
        Assert.AreEqual(1, hover.Removed);
        Assert.AreEqual(1, selected.Applied);
        Assert.AreEqual(1, selected.Removed);
    }

    // reads the end value of the stroke-thickness motion property without depending on the motion clock.
    private static float ThicknessTarget(SolidColorPaint paint) =>
        (float)paint.GetPropertyDefinition(nameof(Paint.StrokeThickness))!.GetMotion(paint)!.ToValue!;
}
