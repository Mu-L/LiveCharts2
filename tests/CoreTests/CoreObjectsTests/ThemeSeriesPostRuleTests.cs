using System.Collections.Generic;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests.CoreObjectsTests;

[TestClass]
public class ThemeSeriesPostRuleTests
{
    [TestMethod]
    public void PostRuleRunsAfterAnyAndTypeSpecificRules()
    {
        // HasPostRuleForAnySeries must run last so it can read what the type-specific builders
        // assigned (e.g. the series' Stroke/Fill). Pin the order: any-series -> type-specific -> post.
        var theme = new Theme();
        var order = new List<string>();

        _ = theme.HasRuleForAnySeries(_ => order.Add("any"));
        _ = theme.HasRuleForLineSeries(_ => order.Add("line"));
        _ = theme.HasPostRuleForAnySeries(_ => order.Add("post"));

        theme.ApplyStyleToSeries(new LineSeries<double>());

        CollectionAssert.AreEqual(new[] { "any", "line", "post" }, order);
    }

    [TestMethod]
    public void PostRuleSeesPaintsAssignedByTypeSpecificRule()
    {
        // The motivating case: a post rule decorating the final paints. The type-specific rule
        // assigns Stroke; the post rule must observe that assignment, not Paint.Default.
        var theme = new Theme();

        _ = theme.HasRuleForLineSeries(s => s.Stroke = new SolidColorPaint());

        var sawStroke = false;
        _ = theme.HasPostRuleForAnySeries(s =>
            sawStroke = s is IStrokedAndFilled { Stroke: not null });

        theme.ApplyStyleToSeries(new LineSeries<double>());

        Assert.IsTrue(sawStroke, "the post rule must see the Stroke assigned by the line-series rule");
    }
}
