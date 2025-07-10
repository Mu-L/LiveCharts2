﻿using System.Windows.Forms;
using LiveChartsCore.SkiaSharpView.Extensions;
using LiveChartsCore.SkiaSharpView.WinForms;
using ViewModelsSamples.Pies.Doughnut;

namespace WinFormsSample.Pies.Doughnut;

public partial class View : UserControl
{
    private readonly PieChart pieChart;

    public View()
    {
        InitializeComponent();
        Size = new System.Drawing.Size(50, 50);

        var seriesCollection = new[] { 2, 4, 1, 4, 3 }
            .AsPieSeries((value, series) =>
            {
                series.MaxRadialColumnWidth = 60;
            });

        pieChart = new PieChart
        {
            Series = seriesCollection,
            Location = new System.Drawing.Point(0, 0),
            Size = new System.Drawing.Size(50, 50),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom
        };

        Controls.Add(pieChart);
    }
}
