﻿using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Painting;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Drawing.Layouts;

namespace ViewModelsSamples.General.TemplatedLegends;

public class LegendItem : StackLayout
{
    public LegendItem(ISeries series, Paint? textPaint)
    {
        Orientation = ContainerOrientation.Horizontal;
        Padding = new Padding(12, 6);
        VerticalAlignment = Align.Middle;
        HorizontalAlignment = Align.Middle;
        Opacity = series.IsVisible ? 1 : 0.5f;

        var miniature = (IDrawnElement<SkiaSharpDrawingContext>)series.GetMiniatureGeometry(null);
        if (miniature is BoundedDrawnGeometry bounded)
            bounded.Height = 40;

        Children = [
            miniature,
            new LabelGeometry
            {
                Text = series.Name ?? "?",
                TextSize = 20,
                Paint = textPaint,
                Padding = new Padding(8, 2, 0, 2),
                VerticalAlign = Align.Start,
                HorizontalAlign = Align.Start
            }
        ];
    }
}
