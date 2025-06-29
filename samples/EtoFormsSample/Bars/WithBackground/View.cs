﻿using Eto.Forms;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Eto;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace EtoFormsSample.Bars.WithBackground;

public class View : Panel
{
    private readonly CartesianChart cartesianChart;

    public View()
    {
        var values1 = new double[] { 10, 10, 10, 10, 10, 10, 10 };
        var values2 = new double[] { 3, 10, 5, 3, 7, 3, 8 };

        var series = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = values1,
                Fill = new SolidColorPaint(new SKColor(180, 180, 180, 50)),
                IgnoresBarPosition = true,
                IsHoverable = false
            },
            new ColumnSeries<double>
            {
                Values = values2,
                IgnoresBarPosition = true
            }
        };

        var yAxis = new Axis
        {
            MinLimit = 0,
            MaxLimit = 10
        };

        cartesianChart = new CartesianChart
        {
            Series = series,
            YAxes = new[] { yAxis },
        };

        Content = cartesianChart;
    }
}
