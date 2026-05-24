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
using System.Runtime.CompilerServices;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.Motion;
using LiveChartsCore.Painting;

namespace LiveChartsCore;

/// <summary>
/// Defines a treemap series. Lays out a tree of nodes inside the chart's
/// draw margin using the squarified treemap algorithm (Bruls, Huijing,
/// van Wijk — 2000). Each node's area is proportional to its weight; the
/// algorithm picks per-row layouts that keep the rectangles as close to
/// square as possible.
/// </summary>
/// <typeparam name="TModel">The user's node type.</typeparam>
/// <typeparam name="TVisual">The per-tile visual (any bounded geometry).</typeparam>
/// <typeparam name="TLabel">The per-tile label geometry.</typeparam>
public abstract class CoreTreemapSeries<TModel, TVisual, TLabel>(
    IReadOnlyCollection<TModel>? values)
        : Series<TModel, TVisual, TLabel>(SeriesProperties.Solid, values), ITreemapSeries
            where TVisual : BoundedDrawnGeometry, new()
            where TLabel : BaseLabelGeometry, new()
{
    private readonly Dictionary<object, TVisual> _nodeVisuals = new(ReferenceComparer.Instance);
    private readonly Dictionary<object, TLabel> _nodeLabels = new(ReferenceComparer.Instance);
    private readonly HashSet<object> _seenThisMeasure = new(ReferenceComparer.Instance);

    /// <summary>Gets or sets the fill paint applied to every tile.</summary>
    public Paint? Fill
    {
        get;
        set => SetPaintProperty(ref field, value);
    } = null;

    /// <summary>Gets or sets the stroke paint applied to every tile.</summary>
    public Paint? Stroke
    {
        get;
        set => SetPaintProperty(ref field, value, PaintStyle.Stroke);
    } = null;

    /// <inheritdoc cref="ITreemapSeries.Padding"/>
    public double Padding { get; set => SetProperty(ref field, value); } = 2;

    /// <inheritdoc cref="ITreemapSeries.CornerRadius"/>
    public double CornerRadius { get; set => SetProperty(ref field, value); }

    /// <inheritdoc cref="ITreemapSeries.AssignedRectangle"/>
    public LvcRectangle AssignedRectangle { get; set; }

    /// <inheritdoc cref="ITreemapSeries.GetTotalWeight"/>
    public double GetTotalWeight()
    {
        if (Values is null || ValueMapper is null) return 0;
        var total = 0.0;
        foreach (var n in Values)
        {
            if (n is null) continue;
            total += ResolveValue(n);
        }
        return total;
    }

    /// <summary>
    /// Resolves the weight of a node. Leaves return their own value; internal
    /// nodes either return their explicit value or fall back to the sum of
    /// their children's resolved weights. Required.
    /// </summary>
    public Func<TModel, double>? ValueMapper { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Returns the children of a node, or <c>null</c> for a leaf. <c>null</c>
    /// on the series itself means "every node is a leaf" (flat treemap).
    /// </summary>
    public Func<TModel, IEnumerable<TModel>?>? ChildrenMapper { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Returns the per-tile label text. Only leaves are labeled — internal
    /// nodes are containers for their children's tiles. Set
    /// <see cref="Series{TModel, TVisual, TLabel}.DataLabelsPaint"/> to opt in.
    /// </summary>
    public Func<TModel, string?>? LabelMapper { get; set => SetProperty(ref field, value); }

    // ---- template method ----------------------------------------------------

    /// <summary>
    /// Builds the per-frame measure context from the chart. Captures the
    /// series's assigned sub-rectangle of the draw margin (the engine
    /// partitions the draw margin by series totals before invalidating each
    /// series) plus per-series cosmetic settings. Falls back to the full draw
    /// margin when the engine hasn't assigned a rectangle yet — covers the
    /// single-series case where no partition is needed.
    /// </summary>
    protected virtual TreemapMeasureContext BeginMeasure(TreemapChartEngine chart)
    {
        var isFirstDraw = !((Chart)chart).IsDrawn(((ISeries)this).SeriesId);

        var assigned = AssignedRectangle;
        var hasAssignment = assigned.Size.Width > 0 && assigned.Size.Height > 0;

        return new TreemapMeasureContext(
            chart,
            drawLocation: hasAssignment ? assigned.Location : chart.DrawMarginLocation,
            drawMarginSize: hasAssignment ? assigned.Size : chart.DrawMarginSize,
            padding: (float)Padding,
            cornerRadius: (float)CornerRadius,
            isFirstDraw: isFirstDraw);
    }

    /// <summary>
    /// Ensures a visual exists for the given node. On first creation seeds the
    /// visual at the tile's center with zero size so the squarified position
    /// is reached via animation.
    /// </summary>
    protected virtual TVisual EnsureVisualForNode(object node, LvcRectangle rect, in TreemapMeasureContext ctx)
    {
        if (_nodeVisuals.TryGetValue(node, out var existing))
            return existing;

        var v = new TVisual
        {
            X = rect.Location.X + rect.Size.Width * 0.5f,
            Y = rect.Location.Y + rect.Size.Height * 0.5f,
            Width = 0,
            Height = 0,
        };
        v.Animate(GetAnimation(ctx.Chart));

        _nodeVisuals[node] = v;
        return v;
    }

    /// <summary>
    /// Applies the squarified rectangle (minus padding) to the visual. The
    /// motion layer tweens to the new values automatically.
    /// </summary>
    protected virtual void UpdateVisualGeometry(TVisual visual, LvcRectangle rect, in TreemapMeasureContext ctx)
    {
        visual.X = rect.Location.X;
        visual.Y = rect.Location.Y;
        visual.Width = rect.Size.Width;
        visual.Height = rect.Size.Height;
        visual.RemoveOnCompleted = false;

        if (visual is BaseRoundedRectangleGeometry rr)
            rr.BorderRadius = new LvcPoint(ctx.CornerRadius, ctx.CornerRadius);
    }

    /// <summary>
    /// Collapses a removed tile to zero size at its center for a clean exit.
    /// </summary>
    protected virtual void CollapseRemovedVisual(TVisual visual)
    {
        var cx = visual.X + visual.Width * 0.5f;
        var cy = visual.Y + visual.Height * 0.5f;
        visual.X = cx;
        visual.Y = cy;
        visual.Width = 0;
        visual.Height = 0;
        visual.RemoveOnCompleted = true;
    }

    /// <inheritdoc cref="ChartElement.Invalidate(Chart)"/>
    public sealed override void Invalidate(Chart chart)
    {
        var treemapChart = (TreemapChartEngine)chart;
        _ = GetAnimation(treemapChart);

        var ctx = BeginMeasure(treemapChart);

        InitializePaints(treemapChart);

        _seenThisMeasure.Clear();

        if (Values is not null)
        {
            var rect = new LvcRectangle(ctx.DrawLocation, ctx.DrawMarginSize);
            LayoutSiblings(Values, rect, in ctx);
        }

        // Reap visuals and labels for nodes that were not visited this measure.
        if (_nodeVisuals.Count != _seenThisMeasure.Count)
        {
            var toRemove = new List<object>();
            foreach (var kv in _nodeVisuals)
            {
                if (_seenThisMeasure.Contains(kv.Key)) continue;
                CollapseRemovedVisual(kv.Value);
                toRemove.Add(kv.Key);
            }
            foreach (var k in toRemove)
            {
                _ = _nodeVisuals.Remove(k);
                if (_nodeLabels.TryGetValue(k, out var label))
                {
                    label.Opacity = 0;
                    label.RemoveOnCompleted = true;
                    _ = _nodeLabels.Remove(k);
                }
            }
        }

        // Also drop labels for tiles that became internal nodes (they had a
        // label, now they shouldn't) or whose LabelMapper started returning null.
        if (_nodeLabels.Count > 0)
        {
            var toDrop = new List<object>();
            foreach (var kv in _nodeLabels)
            {
                if (_seenThisMeasure.Contains(kv.Key) && kv.Value.Opacity > 0) continue;
                if (!_seenThisMeasure.Contains(kv.Key))
                {
                    kv.Value.Opacity = 0;
                    kv.Value.RemoveOnCompleted = true;
                    toDrop.Add(kv.Key);
                }
            }
            foreach (var k in toDrop) _ = _nodeLabels.Remove(k);
        }
    }

    /// <summary>
    /// Sets per-Z-index ordering on Fill / Stroke / DataLabelsPaint and
    /// registers them as drawable tasks on the canvas. Labels sit above
    /// tiles (PieSeriesDataLabelsBaseZIndex offset) and do not clip to the
    /// draw margin.
    /// </summary>
    private void InitializePaints(TreemapChartEngine chart)
    {
        var actualZIndex = ZIndex == 0 ? ((ISeries)this).SeriesId : ZIndex;

        if (Fill is not null && Fill != Paint.Default)
        {
            Fill.ZIndex = actualZIndex + PaintConstants.SeriesFillZIndexOffset;
            chart.Canvas.AddDrawableTask(Fill, zone: CanvasZone.DrawMargin);
        }
        if (Stroke is not null && Stroke != Paint.Default)
        {
            Stroke.ZIndex = actualZIndex + PaintConstants.SeriesStrokeZIndexOffset;
            chart.Canvas.AddDrawableTask(Stroke, zone: CanvasZone.DrawMargin);
        }
        if (ShowDataLabels && DataLabelsPaint is not null && DataLabelsPaint != Paint.Default)
        {
            DataLabelsPaint.ZIndex =
                PaintConstants.PieSeriesDataLabelsBaseZIndex + actualZIndex + PaintConstants.SeriesGeometryFillZIndexOffset;
            chart.Canvas.AddDrawableTask(DataLabelsPaint);
        }
    }

    private void LayoutSiblings(IEnumerable<TModel> siblings, LvcRectangle rect, in TreemapMeasureContext ctx)
    {
        var items = new List<(double weight, TModel node)>();
        foreach (var node in siblings)
        {
            if (node is null) continue;
            items.Add((ResolveValue(node), node));
        }

        var placements = TreemapLayout.Squarify(items, rect);
        foreach (var (node, childRect) in placements)
            EmitTile(node, childRect, in ctx);
    }

    /// <summary>
    /// Resolves a node's weight. Falls back to the sum of its descendants'
    /// resolved weights when the node has children and no explicit non-zero
    /// value of its own.
    /// </summary>
    private double ResolveValue(TModel node)
    {
        if (ValueMapper is null)
            throw new InvalidOperationException(
                $"{nameof(ValueMapper)} is required on {nameof(CoreTreemapSeries<TModel, TVisual, TLabel>)}.");

        var v = ValueMapper(node);
        if (v != 0) return v;

        var children = ChildrenMapper?.Invoke(node);
        if (children is null) return 0;

        var sum = 0.0;
        foreach (var c in children)
        {
            if (c is null) continue;
            sum += ResolveValue(c);
        }
        return sum;
    }

    private void EmitTile(TModel node, LvcRectangle rect, in TreemapMeasureContext ctx)
    {
        // Inset by padding so siblings get a visible gap. Keep dimensions
        // non-negative — a tile thinner than 2 * padding collapses to a line.
        var pad = ctx.Padding;
        var innerW = rect.Size.Width - pad * 2;
        var innerH = rect.Size.Height - pad * 2;
        if (innerW < 0) innerW = 0;
        if (innerH < 0) innerH = 0;
        var inner = new LvcRectangle(
            new LvcPoint(rect.Location.X + pad, rect.Location.Y + pad),
            new LvcSize(innerW, innerH));

        var visual = EnsureVisualForNode(node!, inner, in ctx);
        if (Fill is not null && Fill != Paint.Default)
            Fill.AddGeometryToPaintTask(ctx.Chart.Canvas, visual);
        if (Stroke is not null && Stroke != Paint.Default)
            Stroke.AddGeometryToPaintTask(ctx.Chart.Canvas, visual);

        UpdateVisualGeometry(visual, inner, in ctx);
        _ = _seenThisMeasure.Add(node!);

        var kids = ChildrenMapper?.Invoke(node);
        var isLeaf = kids is null;

        // Labels go on leaves only — internal-node tiles are containers and
        // would just overlap with their children's labels.
        if (isLeaf) MeasureDataLabel(node, inner, in ctx);

        if (!isLeaf) LayoutSiblings(kids!, inner, in ctx);
    }

    /// <summary>
    /// Ensures a label exists for the given leaf, updates its text + paint,
    /// and centers it inside the tile. No-op when the tile is too small to
    /// fit the label or when label rendering is disabled.
    /// </summary>
    private void MeasureDataLabel(TModel node, LvcRectangle rect, in TreemapMeasureContext ctx)
    {
        if (!ShowDataLabels || DataLabelsPaint is null || DataLabelsPaint == Paint.Default || LabelMapper is null)
            return;

        var text = LabelMapper(node);
        if (string.IsNullOrEmpty(text)) return;

        var cx = rect.Location.X + rect.Size.Width * 0.5f;
        var cy = rect.Location.Y + rect.Size.Height * 0.5f;

        if (!_nodeLabels.TryGetValue(node!, out var label))
        {
            label = new TLabel
            {
                X = cx,
                Y = cy,
                HorizontalAlign = Align.Middle,
                VerticalAlign = Align.Middle,
                MaxWidth = (float)DataLabelsMaxWidth,
            };
            label.Animate(GetAnimation(ctx.Chart), BaseLabelGeometry.XProperty, BaseLabelGeometry.YProperty);
            _nodeLabels[node!] = label;
        }

        DataLabelsPaint.AddGeometryToPaintTask(ctx.Chart.Canvas, label);

        label.Text = text!;
        label.TextSize = (float)DataLabelsSize;
        label.Padding = DataLabelsPadding;
        label.Paint = DataLabelsPaint;

        // Cull labels that won't fit. Measure() gives the rendered bounds;
        // a tile narrower than the text just renders a blank rect.
        var measured = label.Measure();
        if (measured.Width > rect.Size.Width || measured.Height > rect.Size.Height)
        {
            label.Opacity = 0;
            return;
        }
        label.Opacity = 1;
        label.X = cx;
        label.Y = cy;
    }

    // ---- Series abstract members (image-gen scope: stubs / no-ops) ----------

    /// <inheritdoc cref="ISeries.GetPrimaryToolTipText(ChartPoint)"/>
    public override string? GetPrimaryToolTipText(ChartPoint point) => null;

    /// <inheritdoc cref="ISeries.GetSecondaryToolTipText(ChartPoint)"/>
    public override string? GetSecondaryToolTipText(ChartPoint point) => LiveCharts.IgnoreToolTipLabel;

    /// <inheritdoc cref="Series{TModel, TVisual, TLabel}.GetMiniatureGeometry(ChartPoint?)"/>
    public override IDrawnElement GetMiniatureGeometry(ChartPoint? point) =>
        new TVisual
        {
            Fill = Fill,
            Stroke = Stroke,
            StrokeThickness = (float)MiniatureStrokeThickness,
            ClippingBounds = LvcRectangle.Empty,
            Width = (float)MiniatureShapeSize,
            Height = (float)MiniatureShapeSize,
        };

    /// <inheritdoc cref="ChartElement.GetPaintTasks"/>
    protected internal override Paint?[] GetPaintTasks() => [Fill, Stroke, DataLabelsPaint];

    /// <inheritdoc cref="Series{TModel, TVisual, TLabel}.SetDefaultPointTransitions(ChartPoint)"/>
    protected override void SetDefaultPointTransitions(ChartPoint chartPoint)
    {
        // No-op: treemap visuals are managed directly via _nodeVisuals and animated
        // in EnsureVisualForNode; no ChartPoint round-trip.
    }

    /// <inheritdoc cref="ISeries.SoftDeleteOrDispose(IChartView)"/>
    public override void SoftDeleteOrDispose(IChartView chart)
    {
        var core = ((ITreemapChartView)chart).Core;

        foreach (var visual in _nodeVisuals.Values)
            CollapseRemovedVisual(visual);
        _nodeVisuals.Clear();
        foreach (var label in _nodeLabels.Values)
        {
            label.Opacity = 0;
            label.RemoveOnCompleted = true;
        }
        _nodeLabels.Clear();
        _seenThisMeasure.Clear();

        foreach (var pt in GetPaintTasks())
            if (pt is not null) core.Canvas.RemovePaintTask(pt);
    }

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public static ReferenceComparer Instance { get; } = new();
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
