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

using Factos;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.Themes;
using SharedUITests.Helpers;
using Xunit;

#if WINUI_UI_TESTING || UNO_UI_TESTING
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
#endif

namespace SharedUITests;

public class ThemeTests
{
    public AppController App => AppController.Current;

    // Chart-level theming (HasRuleForChart) styles the IChartView itself from the shared
    // theme logic, across every platform. This runs on all UI platforms: the themed value
    // must reach the view on all of them. The user-set-wins arbitration lives in the
    // generated dependency-property setters, so that assertion is scoped to XAML platforms
    // (WPF / Avalonia / WinUI / MAUI / Uno) where it is implemented; the non-XAML
    // arbitration is a tracked follow-up.
    [AppTestMethod]
    public async Task ChartLevelThemeAppliesAndRespectsUserSet()
    {
        try
        {
            LiveCharts.Configure(c => c.AddDefaultTheme(theme => theme
                .HasRuleForChart(view =>
                {
                    view.TooltipTextSize = 41;
                    view.LegendTextSize = 7;
                })));

            var sut = await App.NavigateTo<Samples.General.FirstChart.View>();
            // WaitUntilChartLoadsAndRenders polls with a timeout; the unbounded
            // WaitUntilChartRenders races UpdateStarted against its own subscription
            // and hangs on Android when the forced, un-throttled update fires the
            // event before the handler is attached — which stalls the whole Factos
            // session, not just this test.
            await sut.Chart.WaitUntilChartLoadsAndRenders();

            var view = (IChartView)sut.Chart;

            // user explicitly sets LegendTextSize, then force another theme pass.
            await view.InvokeOnUIThreadAsync(() =>
            {
                view.LegendTextSize = 99;
                sut.Chart.CoreChart.Update();
            });

            await sut.Chart.WaitUntilChartLoadsAndRenders();
            await Task.Delay(500);

            await view.InvokeOnUIThreadAsync(() =>
            {
                // the theme reaches the view on every platform.
                Assert.Equal(41d, view.TooltipTextSize, 3);

#if XAML_UI_TESTING
                // the theme must not overwrite a value the user set (DP-setter arbitration).
                Assert.Equal(99d, view.LegendTextSize, 3);
#endif
            });
        }
        finally
        {
            // restore the default theme so the rule does not leak into other tests.
            LiveCharts.Configure(c => c.AddSkiaSharp());
        }
    }

#if WINUI_UI_TESTING || UNO_UI_TESTING
    // regression for https://github.com/Live-Charts/LiveCharts2/issues/2004
    //
    // The chart used to read Application.Current.RequestedTheme, which only
    // tracks the App.xaml-time setting and ignores element-level overrides
    // (FrameworkElement.RequestedTheme on a Window, page, ancestor, or the
    // chart itself — the standard WinUI 3 idiom for theme switching). The
    // fix routes IsDarkMode through this.ActualTheme so any link in the
    // resolution chain triggers the right palette. This canary sets
    // RequestedTheme on the chart directly (the most pessimistic case —
    // there is no Application.RequestedTheme=Dark to fall back on) and
    // asserts IsDarkMode flips with it.
    [AppTestMethod]
    public async Task WinUI_isDarkMode_honors_element_RequestedTheme()
    {
        var sut = await App.NavigateTo<Samples.General.FirstChart.View>();
        await sut.Chart.WaitUntilChartRenders();

        var chart = (FrameworkElement)sut.Chart;
        var view = (IChartView)sut.Chart;

        await RunOnDispatcherAsync(chart, () =>
        {
            chart.RequestedTheme = ElementTheme.Dark;
            Assert.True(
                view.IsDarkMode,
                "IsDarkMode must follow the chart's resolved ActualTheme; setting RequestedTheme=Dark on the chart (or any ancestor FrameworkElement) is the standard WinUI 3 way to opt a subtree into dark mode and Application.RequestedTheme — the property the chart used to read — does not change in response.");

            chart.RequestedTheme = ElementTheme.Light;
            Assert.False(
                view.IsDarkMode,
                "Flipping RequestedTheme back to Light must flip IsDarkMode back to false; otherwise the chart would stay locked on the first theme it observed and not respond to runtime theme toggles.");
        });
    }

    private static Task RunOnDispatcherAsync(FrameworkElement chart, Action work)
    {
        var tcs = new TaskCompletionSource<object?>();
        if (!chart.DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                work();
                tcs.SetResult(null);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }))
        {
            tcs.SetException(new InvalidOperationException("Failed to enqueue work on the chart's DispatcherQueue."));
        }
        return tcs.Task;
    }
#endif
}
