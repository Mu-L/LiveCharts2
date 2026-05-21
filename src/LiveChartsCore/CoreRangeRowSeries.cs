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

using System;
using System.Collections.Generic;
using LiveChartsCore.Drawing;
using LiveChartsCore.Drawing.Segments;
using LiveChartsCore.Kernel;
using LiveChartsCore.Measure;
using LiveChartsCore.Painting;

namespace LiveChartsCore;

/// <summary>
/// Defines a row-shaped series whose rectangles span <c>[Low, High]</c> on the
/// value axis instead of <c>[Pivot, Value]</c>. Horizontal mirror of
/// <see cref="CoreRangeColumnSeries{TModel, TVisual, TLabel, TErrorGeometry}"/>:
/// the primary axis is X, so the [Low, High] pair drives left/right edges.
/// Stacking is not supported.
/// </summary>
public abstract class CoreRangeRowSeries<TModel, TVisual, TLabel, TErrorGeometry>
    : HorizontalBarSeries<TModel, TVisual, TLabel, TErrorGeometry>
        where TVisual : BoundedDrawnGeometry, new()
        where TLabel : BaseLabelGeometry, new()
        where TErrorGeometry : BaseLineGeometry, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoreRangeRowSeries{TModel, TVisual, TLabel, TErrorGeometry}"/> class.
    /// </summary>
    protected CoreRangeRowSeries(IReadOnlyCollection<TModel>? values)
        : base(values, isStacked: false)
    {
    }

    /// <inheritdoc cref="BarSeries{TModel, TVisual, TLabel}.MeasureBarLayout(ChartPoint, in BarMeasureContext)"/>
    protected override BarLayout MeasureBarLayout(ChartPoint point, in BarMeasureContext ctx)
    {
        var coordinate = point.Coordinate;
        var helper = ctx.Helper;

        // For HorizontalBarSeries: PrimaryAxis = X (value), SecondaryAxis = Y (category).
        // PrimaryValue carries High → right edge; TertiaryValue carries Low → left edge.
        var highPx = ctx.PrimaryScale.ToPixels(coordinate.PrimaryValue);
        var lowPx = ctx.PrimaryScale.ToPixels(coordinate.TertiaryValue);
        var secondary = ctx.SecondaryScale.ToPixels(coordinate.SecondaryValue);

        var x = Math.Min(lowPx, highPx);
        var w = Math.Abs(highPx - lowPx);
        var y = secondary - helper.uwm + helper.cp;

        return new BarLayout(
            x: x, y: y, width: w, height: helper.uw,
            categoryHoverX: x, categoryHoverY: secondary - helper.actualUw * 0.5f,
            categoryHoverWidth: w, categoryHoverHeight: helper.actualUw);
    }

    /// <inheritdoc cref="BarSeries{TModel, TVisual, TLabel}.EnsureVisualForPoint(ChartPoint, in BarMeasureContext)"/>
    protected override TVisual EnsureVisualForPoint(ChartPoint point, in BarMeasureContext ctx)
    {
        var visual = point.Context.Visual as TVisual;
        var coordinate = point.Coordinate;

        // Range bars don't carry error visuals in this initial implementation;
        // a re-attach (visual already exists from a prior frame) is a no-op.
        if (visual is not null) return visual;

        // Enter at the midpoint of [Low, High] at zero width — symmetric to the
        // CoreRangeColumnSeries vertical counterpart.
        var helper = ctx.IsFirstDraw ? ctx.Helper : ctx.PreviousHelper;
        var secondaryScale = ctx.IsFirstDraw ? ctx.SecondaryScale : ctx.PreviousSecondaryScale;
        var primaryScale = ctx.IsFirstDraw ? ctx.PrimaryScale : ctx.PreviousPrimaryScale;

        var highPx = primaryScale.ToPixels(coordinate.PrimaryValue);
        var lowPx = primaryScale.ToPixels(coordinate.TertiaryValue);
        var midPx = (highPx + lowPx) * 0.5f;
        var yi = secondaryScale.ToPixels(coordinate.SecondaryValue) - helper.uwm + helper.cp;
        var uwi = helper.uw;

        var r = new TVisual
        {
            X = midPx,
            Y = yi,
            Width = 0f,
            Height = uwi,
        };

        if (r is BaseRoundedRectangleGeometry rg)
            rg.BorderRadius = new LvcPoint(ctx.Rx, ctx.Ry);

        point.Context.Visual = r;
        OnPointCreated(point);

        _ = everFetched.Add(point);

        return r;
    }

    /// <inheritdoc cref="HorizontalBarSeries{TModel, TVisual, TLabel, TErrorGeometry}.SoftDeleteOrDisposePoint(ChartPoint, Scaler, Scaler)"/>
    protected internal override void SoftDeleteOrDisposePoint(ChartPoint point, Scaler primaryScale, Scaler secondaryScale)
    {
        var visual = (TVisual?)point.Context.Visual;
        if (visual is null) return;
        if (DataFactory is null) throw new Exception("Data provider not found");

        var coordinate = point.Coordinate;
        var highPx = primaryScale.ToPixels(coordinate.PrimaryValue);
        var lowPx = primaryScale.ToPixels(coordinate.TertiaryValue);
        var midPx = (highPx + lowPx) * 0.5f;
        var secondary = secondaryScale.ToPixels(coordinate.SecondaryValue);

        visual.X = midPx;
        visual.Y = secondary - visual.Height * 0.5f;
        visual.Width = 0;
        visual.RemoveOnCompleted = true;

        DataFactory.DisposePoint(point);

        var label = (TLabel?)point.Context.Label;
        if (label is null) return;

        label.TextSize = 1;
        label.RemoveOnCompleted = true;
    }

    /// <summary>
    /// Range row hover/tooltip uses both endpoints — defaults to "L: low  H: high".
    /// </summary>
    /// <inheritdoc cref="ISeries.GetPrimaryToolTipText(ChartPoint)"/>
    public override string? GetPrimaryToolTipText(ChartPoint point) =>
        $"L: {point.Coordinate.TertiaryValue}, H: {point.Coordinate.PrimaryValue}";
}
