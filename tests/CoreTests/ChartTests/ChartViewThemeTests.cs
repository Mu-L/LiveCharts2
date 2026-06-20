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
            LiveCharts.Configure(s => s.AddSkiaSharp());
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
            LiveCharts.Configure(s => s.AddSkiaSharp());

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
            LiveCharts.Configure(s => s.AddSkiaSharp());
        }
    }
}
