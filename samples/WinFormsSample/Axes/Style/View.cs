﻿using System.Drawing;
using System.Windows.Forms;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using LiveChartsCore.SkiaSharpView.WinForms;
using SkiaSharp;

#pragma warning disable IDE1006 // Naming Styles

namespace WinFormsSample.Axes.Style;

public partial class View : UserControl
{
    private readonly CartesianChart cartesianChart;

    public View()
    {
        InitializeComponent();
        Size = new Size(100, 100);

        var values = new ObservablePoint[1001];
        var fx = EasingFunctions.BounceInOut;
        for (var i = 0; i < 1001; i++)
        {
            var x = i / 1000f;
            var y = fx(x);
            values[i] = new ObservablePoint(x - 0.5, y - 0.5);
        }

        var gray = new SKColor(195, 195, 195);
        var gray1 = new SKColor(160, 160, 160);
        var gray2 = new SKColor(90, 90, 90);
        var gray3 = new SKColor(60, 60, 60);

        var series = new ISeries[]
        {
            new LineSeries<ObservablePoint>
            {
                Values = values,
                Stroke = new SolidColorPaint(new SKColor(33, 150, 243), 4), // #2196F3
                Fill = null,
                GeometryFill = null,
                GeometryStroke = null
            }
        };

        var dashEffect = new DashEffect([3, 3]);

        var xAxis = new Axis
        {
            Name = "X Axis",
            NamePaint = new SolidColorPaint(gray1),
            TextSize = 18,
            Padding = new LiveChartsCore.Drawing.Padding(5, 15, 5, 5),
            LabelsPaint = new SolidColorPaint(gray),
            SeparatorsPaint = new SolidColorPaint(gray, 1) { PathEffect = dashEffect },
            SubseparatorsPaint = new SolidColorPaint(gray2, 0.5f),
            SubseparatorsCount = 9,
            ZeroPaint = new SolidColorPaint(gray1, 2),
            TicksPaint = new SolidColorPaint(gray, 1.5f),
            SubticksPaint = new SolidColorPaint(gray, 1)
        };
        var yAxis = new Axis
        {
            Name = "Y Axis",
            NamePaint = new SolidColorPaint(gray1),
            TextSize = 18,
            Padding = new LiveChartsCore.Drawing.Padding(5, 15, 5, 5),
            LabelsPaint = new SolidColorPaint(gray),
            SeparatorsPaint = new SolidColorPaint(gray, 1) { PathEffect = dashEffect },
            SubseparatorsPaint = new SolidColorPaint(gray2, 0.5f),
            SubseparatorsCount = 9,
            ZeroPaint = new SolidColorPaint(gray1, 2),
            TicksPaint = new SolidColorPaint(gray, 1.5f),
            SubticksPaint = new SolidColorPaint(gray, 1)
        };

        var frame = new DrawMarginFrame
        {
            Stroke = new SolidColorPaint(gray, 2)
        };

        cartesianChart = new CartesianChart
        {
            BackColor = Color.FromArgb(255, 48, 48, 48),
            Series = series,
            XAxes = [xAxis],
            YAxes = [yAxis],
            DrawMarginFrame = frame,
            ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.Both,
            TooltipPosition = LiveChartsCore.Measure.TooltipPosition.Hidden,
            Location = new Point(0, 0),
            Size = new Size(100, 100),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom
        };

        Controls.Add(cartesianChart);
    }
}
