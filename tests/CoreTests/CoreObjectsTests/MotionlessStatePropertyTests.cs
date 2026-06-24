using System.Collections.Generic;
using LiveChartsCore.Drawing;
using LiveChartsCore.Motion;
using LiveChartsCore.VisualStates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static LiveChartsCore.VisualStates.VisualStatesDictionary;

namespace CoreTests.CoreObjectsTests;

// Visual states no longer require a property to be a motion property: a PropertyDefinition with a null
// motionGetter (the shape a future [StateProperty] attribute will emit) can be targeted by a state.
// The original value is snapshotted through the definition getter instead of a motion's ToValue, so
// the property is set on apply and restored on clear, exactly like a motion-backed one.
[TestClass]
public class MotionlessStatePropertyTests
{
    // an Animatable whose "Label" is a plain property (no motion) registered as a motionless definition.
    private sealed class TestAnimatable : Animatable
    {
        public string? Label { get; set; }

        private static readonly Dictionary<string, PropertyDefinition> s_definitions = new()
        {
            ["Label"] = new PropertyDefinition(
                "Label",
                typeof(string),
                getter: g => ((TestAnimatable)g).Label,
                setter: (g, v) => ((TestAnimatable)g).Label = (string?)v,
                motionGetter: _ => null) // <- no motion property backs this definition
        };

        protected override Dictionary<string, PropertyDefinition> GetPropertyDefinitions() => s_definitions;
    }

    private static VisualStatesDictionary StateSetting(string name, string property, object value) =>
        new()
        {
            [name] = new DrawnPropertiesDictionary(
                new Dictionary<string, DrawnPropertySetter> { [property] = new DrawnPropertySetter(property, value) },
                isInternalSet: false)
        };

    [TestMethod]
    public void SetState_AppliesAndClearState_RestoresAMotionlessProperty()
    {
        var states = StateSetting("Hover", "Label", "hovered");
        var target = new TestAnimatable { Label = "default" };

        states.SetState("Hover", target);
        Assert.AreEqual("hovered", target.Label, "a state must set a motionless registered property.");

        states.ClearState("Hover", target);
        Assert.AreEqual("default", target.Label, "clearing must restore the snapshotted original value.");
    }

    [TestMethod]
    public void ClearStates_RestoresAMotionlessProperty()
    {
        var states = StateSetting("Hover", "Label", "hovered");
        var target = new TestAnimatable { Label = "default" };

        states.SetState("Hover", target);
        states.ClearStates(target);

        Assert.AreEqual("default", target.Label);
    }

    [TestMethod]
    public void Layered_MotionlessStates_RestoreInOrder()
    {
        var states = new VisualStatesDictionary
        {
            ["Hover"] = new DrawnPropertiesDictionary(
                new Dictionary<string, DrawnPropertySetter> { ["Label"] = new DrawnPropertySetter("Label", "hover") },
                isInternalSet: false),
            ["Selected"] = new DrawnPropertiesDictionary(
                new Dictionary<string, DrawnPropertySetter> { ["Label"] = new DrawnPropertySetter("Label", "selected") },
                isInternalSet: false)
        };

        var target = new TestAnimatable { Label = "default" };

        states.SetState("Hover", target);
        states.SetState("Selected", target);
        Assert.AreEqual("selected", target.Label);

        // removing the top state falls back to the still-active one, not the original.
        states.ClearState("Selected", target);
        Assert.AreEqual("hover", target.Label);

        // removing the last active state restores the original snapshot.
        states.ClearState("Hover", target);
        Assert.AreEqual("default", target.Label);
    }
}
