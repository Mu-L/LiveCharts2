﻿using System;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Themes;
using SkiaSharp;

namespace ViewModelsSamples;

public static class LiveChartsThemeExtensions
{
    // now in the AddMyCustomTheme function, we use the default theme // mark
    // but we also override some properties, like the animations speed, easing function and colors // mark
    public static LiveChartsSettings AddMyCustomTheme(this LiveChartsSettings settings) =>
        settings.AddDefaultTheme(theme => theme
            .OnInitialized(() =>
            {
                // the OnInitialized method is called when the // mark
                // theme is applied to a chart for the first time // mark
                // here ee can define colors, animations speed, easing function, etc. // mark
                theme.AnimationsSpeed = TimeSpan.FromSeconds(1);
                theme.EasingFunction = EasingFunctions.BounceOut;
                theme.Colors = [
                    new(66, 165, 245),
                    new(30, 136, 229),
                    new(21, 101, 192)
                ];
            })
            .HasRuleForBarSeries(series =>
            {
                // this method is called when styling a column or row series // mark
                // here we define the corner radius // mark
                series.Rx = 10;
                series.Ry = 10;
            })
            .HasRuleForAxes(axis =>
            {
                if (axis is not ICartesianAxis cartesianAxis) return;

                // Define the color based on the system and app theme // mark
                // this only works on WinUI, Avalonia, Uno and Maui // mark
                // because these frameworks provide a way to detect the system theme // mark
                SKColor lineColor = theme.IsDark
                    ? new(90, 90, 90)
                    : new(235, 235, 235);

                axis.SeparatorsPaint = new SolidColorPaint(lineColor, 2);
                axis.SeparatorsPaint = new SolidColorPaint(lineColor, 1);
            })

        // for more information about the theme configuration, please see the // mark
        // source code of the default theme: // mark
        // https://github.com/beto-rodriguez/LiveCharts2/blob/master/src/skiasharp/LiveChartsCore.SkiaSharp/ThemesExtensions.cs

        );
}
