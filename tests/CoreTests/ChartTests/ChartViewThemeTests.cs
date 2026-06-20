// The MIT License(MIT)
//
// Copyright(c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using LiveChartsCore.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests.ChartTests;

// Covers the shared chart-level theming added with HasRuleForChart: theme rules that
// target the IChartView itself (legend / tooltip paints and sizes, animations speed, ...)
// must run during measure, and the per-instance ChartTheme property must be honored.
//
// These run against the in-memory SK chart, which exercises the same engines + the new
// IChartView.ApplyTheme call site. The user-set-wins arbitration depends on the generated
// dependency-property setters (XAML platforms), so it is covered by the Factos UI test in
// tests/SharedUITests/ThemeTests.cs, not here.
[TestClass]
public class ChartViewThemeTests
{
    private static Theme BuildThemeWith(System.Action<Theme> extra)
    {
        LiveCharts.Configure(s => s.AddDefaultTheme(extra));
        return LiveCharts.DefaultSettings.GetTheme();
    }

    // AddSkiaSharp only registers the parsers + provider; it does NOT rebuild the
    // theme, so it never clears a HasRuleForChart rule from the global default.
    // AddDefaultTheme rebuilds a fresh Theme (empty ChartBuilder), which is what
    // actually restores the default and stops a rule leaking into other tests.
    private static void ResetTheme() =>
        LiveCharts.Configure(s => s.AddDefaultTheme());

    [TestMethod]
    public void ChartLevelRuleIsAppliedDuringMeasure()
    {
        try
        {
            _ = BuildThemeWith(t => t.HasRuleForChart(view => view.TooltipTextSize = 41));

            var chart = new SKCartesianChart
            {
                Width = 300,
                Height = 300,
                Series = [new LineSeries<double> { Values = [1, 2, 3] }]
            };

            _ = chart.GetImage();

            Assert.AreEqual(
                41d, ((IChartView)chart).TooltipTextSize,
                "HasRuleForChart must run during measure and set chart-level view properties.");
        }
        finally
        {
            // restore the default theme so the rule does not leak into other tests.
            ResetTheme();
        }
    }

    [TestMethod]
    public void ChartLevelRuleReachesGeoMap()
    {
        // Regression: GeoMapChart.Measure discarded GetTheme() and never called
        // IChartView.ApplyTheme, so chart-level rules never reached geo map views.
        try
        {
            _ = BuildThemeWith(t => t.HasRuleForChart(view => view.TooltipTextSize = 33));

            var map = new SKGeoMap
            {
                Width = 300,
                Height = 300,
                ExplicitDisposing = true,
                Series = [new HeatLandSeries { Lands = [new() { Name = "fra", Value = 10 }] }]
            };

            _ = map.GetImage();

            Assert.AreEqual(
                33d, ((IChartView)map).TooltipTextSize,
                "HasRuleForChart must reach geo map views via GeoMapChart.Measure's ApplyTheme call.");
        }
        finally
        {
            ResetTheme();
        }
    }

    [TestMethod]
    public void ChartThemePropertyIsHonored()
    {
        try
        {
            // Capture a theme whose chart rule sets a distinct sentinel, then reset the
            // global default so the ONLY source of that sentinel is this per-instance theme.
            var perInstanceTheme = BuildThemeWith(t => t.HasRuleForChart(view => view.TooltipTextSize = 37));
            ResetTheme();

            var chart = new SKCartesianChart
            {
                Width = 300,
                Height = 300,
                Series = [new LineSeries<double> { Values = [1, 2, 3] }],
                ChartTheme = perInstanceTheme
            };

            _ = chart.GetImage();

            Assert.AreEqual(
                37d, ((IChartView)chart).TooltipTextSize,
                "The chart must use the theme assigned to its ChartTheme property, not the global default.");
        }
        finally
        {
            ResetTheme();
        }
    }

    [TestMethod]
    public void ChartLevelRuleForDrawMarginHonoredInSameMeasurePass()
    {
        // Regression: the cartesian and polar engines read view.DrawMargin into a
        // local BEFORE calling ApplyTheme, so a HasRuleForChart rule that set
        // DrawMargin was ignored for that measure pass (GetImage measures exactly
        // once). The pie / sankey / treemap engines already read it after ApplyTheme;
        // this pins the same order for cartesian and polar. A 300x300 control with a
        // themed 40px margin on every side must produce a (40,40) draw-margin origin
        // and a 220x220 draw-margin size.
        try
        {
            // Capture a theme carrying the DrawMargin rule, then immediately reset the
            // global default. A global DrawMargin rule would force a 40px margin on
            // EVERY chart measured during the run (SK charts have no user-set-wins
            // arbitration), corrupting unrelated tests such as CollapsedDrawMargin* and
            // PolarDrawMargin. Applying it only via the per-instance ChartTheme keeps it
            // scoped to the two charts under test.
            var theme = BuildThemeWith(t => t.HasRuleForChart(view => view.DrawMargin = new Margin(40)));
            ResetTheme();

            var cartesian = new SKCartesianChart
            {
                Width = 300,
                Height = 300,
                Series = [new LineSeries<double> { Values = [1, 2, 3] }],
                ChartTheme = theme
            };

            _ = cartesian.GetImage();

            var cartesianCore = ((IChartView)cartesian).CoreChart;
            Assert.AreEqual(
                40f, cartesianCore.DrawMarginLocation.X, 0.5f,
                "Cartesian: a HasRuleForChart DrawMargin must be honored in the same measure pass.");
            Assert.AreEqual(40f, cartesianCore.DrawMarginLocation.Y, 0.5f);
            Assert.AreEqual(220f, cartesianCore.DrawMarginSize.Width, 0.5f);
            Assert.AreEqual(220f, cartesianCore.DrawMarginSize.Height, 0.5f);

            var polar = new SKPolarChart
            {
                Width = 300,
                Height = 300,
                Series = [new PolarLineSeries<double> { Values = [1, 2, 3] }],
                ChartTheme = theme
            };

            _ = polar.GetImage();

            var polarCore = ((IChartView)polar).CoreChart;
            Assert.AreEqual(
                40f, polarCore.DrawMarginLocation.X, 0.5f,
                "Polar: a HasRuleForChart DrawMargin must be honored in the same measure pass.");
            Assert.AreEqual(40f, polarCore.DrawMarginLocation.Y, 0.5f);
            Assert.AreEqual(220f, polarCore.DrawMarginSize.Width, 0.5f);
            Assert.AreEqual(220f, polarCore.DrawMarginSize.Height, 0.5f);
        }
        finally
        {
            ResetTheme();
        }
    }
}
