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
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.Motion;
using LiveChartsCore.Painting;

namespace LiveChartsCore;

/// <summary>
/// Defines a sankey series. The graph topology (Values = nodes,
/// <see cref="Links"/> = edges) is laid out via the d3-sankey algorithm
/// (<see cref="SankeyLayout"/>) on every measure pass.
/// </summary>
/// <typeparam name="TNode">The user's node type.</typeparam>
/// <typeparam name="TVisual">The per-node visual (any bounded geometry).</typeparam>
/// <typeparam name="TLabel">The per-node label geometry.</typeparam>
public abstract class CoreSankeySeries<TNode, TVisual, TLabel>(
    IReadOnlyCollection<TNode>? values)
        : Series<TNode, TVisual, TLabel>(SeriesProperties.Solid, values), ISankeySeries
            where TNode : notnull
            where TVisual : BoundedDrawnGeometry, new()
            where TLabel : BaseLabelGeometry, new()
{
    private readonly Dictionary<TNode, TVisual> _nodeVisuals = new(ReferenceComparer<TNode>.Instance);
    private readonly Dictionary<SankeyLink<TNode>, BaseSankeyRibbonGeometry> _linkVisuals =
        new(ReferenceComparer<SankeyLink<TNode>>.Instance);
    private readonly Dictionary<TNode, TLabel> _nodeLabels = new(ReferenceComparer<TNode>.Instance);

    /// <summary>Gets or sets the fill paint applied to every node rectangle.</summary>
    public Paint? Fill
    {
        get;
        set => SetPaintProperty(ref field, value);
    } = null;

    /// <summary>Gets or sets the stroke paint applied to every node rectangle.</summary>
    public Paint? Stroke
    {
        get;
        set => SetPaintProperty(ref field, value, PaintStyle.Stroke);
    } = null;

    /// <summary>
    /// Gets or sets the fill paint applied to every flow ribbon. Typically a
    /// tinted/translucent version of <see cref="Fill"/>; when null, the
    /// ribbons inherit <see cref="Fill"/> directly.
    /// </summary>
    public Paint? LinkFill
    {
        get;
        set => SetPaintProperty(ref field, value);
    } = null;

    /// <inheritdoc cref="ISankeySeries.NodeWidth"/>
    public double NodeWidth { get; set => SetProperty(ref field, value); } = 12;

    /// <inheritdoc cref="ISankeySeries.NodePadding"/>
    public double NodePadding { get; set => SetProperty(ref field, value); } = 8;

    /// <inheritdoc cref="ISankeySeries.NodeCornerRadius"/>
    public double NodeCornerRadius { get; set => SetProperty(ref field, value); }

    /// <inheritdoc cref="ISankeySeries.LayoutIterations"/>
    public int LayoutIterations { get; set => SetProperty(ref field, value); } = 32;

    /// <inheritdoc cref="ISankeySeries.LinkOpacity"/>
    public double LinkOpacity { get; set => SetProperty(ref field, value); } = 0.5;

    /// <summary>
    /// The graph's edges. Each <see cref="SankeyLink{TNode}"/>.Source and .Target
    /// must reference instances present in <see cref="Series{TModel, TVisual, TLabel}.Values"/>;
    /// links pointing at unknown nodes are silently dropped by the layout.
    /// </summary>
    public IEnumerable<SankeyLink<TNode>>? Links { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Returns the per-node label text. Set
    /// <see cref="Series{TModel, TVisual, TLabel}.DataLabelsPaint"/> to opt
    /// in. When null and TNode is the built-in <see cref="SankeyNode"/>,
    /// <see cref="SankeyNode.Name"/> is used automatically.
    /// </summary>
    public Func<TNode, string?>? LabelMapper { get; set => SetProperty(ref field, value); }

    // ---- template method ----------------------------------------------------

    /// <summary>Builds the per-frame measure context.</summary>
    protected virtual SankeyMeasureContext BeginMeasure(SankeyChartEngine chart)
    {
        var isFirstDraw = !((Chart)chart).IsDrawn(((ISeries)this).SeriesId);

        return new SankeyMeasureContext(
            chart,
            drawLocation: chart.DrawMarginLocation,
            drawMarginSize: chart.DrawMarginSize,
            nodeWidth: (float)NodeWidth,
            nodePadding: (float)NodePadding,
            layoutIterations: LayoutIterations,
            linkOpacity: (float)LinkOpacity,
            isFirstDraw: isFirstDraw);
    }

    /// <summary>Ensures a visual exists for a node — seeds at its target rect center with zero size for entrance animation.</summary>
    private TVisual EnsureVisualForNode(TNode node, SankeyLayout.NodeBox box)
    {
        if (_nodeVisuals.TryGetValue(node, out var existing)) return existing;
        var v = new TVisual
        {
            X = box.X + box.Width * 0.5f,
            Y = box.Y + box.Height * 0.5f,
            Width = 0,
            Height = 0,
        };
        _nodeVisuals[node] = v;
        return v;
    }

    /// <summary>Ensures a ribbon geometry exists for a link — seeded collapsed at the source midpoint.</summary>
    private BaseSankeyRibbonGeometry EnsureVisualForLink(SankeyLink<TNode> link, SankeyLayout.RibbonBox<TNode> box)
    {
        if (_linkVisuals.TryGetValue(link, out var existing)) return existing;

        var midY = (box.SourceY0 + box.SourceY1) * 0.5f;
        var v = CreateRibbonVisual();
        v.SourceX = box.SourceX;
        v.SourceY0 = midY;
        v.SourceY1 = midY;
        v.TargetX = box.TargetX;
        v.TargetY0 = midY;
        v.TargetY1 = midY;
        _linkVisuals[link] = v;
        return v;
    }

    /// <summary>Factory hook — subclasses override to pick the ribbon visual concrete type (default RoundedRectangleGeometry-shaped sibling per platform).</summary>
    protected abstract BaseSankeyRibbonGeometry CreateRibbonVisual();

    /// <inheritdoc cref="ChartElement.Invalidate(Chart)"/>
    public sealed override void Invalidate(Chart chart)
    {
        var sankeyChart = (SankeyChartEngine)chart;
        var anim = GetAnimation(sankeyChart);
        var ctx = BeginMeasure(sankeyChart);

        InitializePaints(sankeyChart);

        if (Values is null || Values.Count == 0)
        {
            ReapAll();
            return;
        }

        var rect = new LvcRectangle(ctx.DrawLocation, ctx.DrawMarginSize);
        var layout = SankeyLayout.Compute(
            Values,
            Links ?? [],
            rect,
            ctx.NodeWidth,
            ctx.NodePadding,
            ctx.LayoutIterations);

        // Nodes
        var seenNodes = new HashSet<TNode>(ReferenceComparer<TNode>.Instance);
        var chartCenterX = rect.Location.X + rect.Size.Width * 0.5f;
        foreach (var kv in layout.Nodes)
        {
            var node = kv.Key;
            var box = kv.Value;
            var visual = EnsureVisualForNode(node, box);
            if (Fill is not null && Fill != Paint.Default)
                Fill.AddGeometryToPaintTask(sankeyChart.Canvas, visual);
            if (Stroke is not null && Stroke != Paint.Default)
                Stroke.AddGeometryToPaintTask(sankeyChart.Canvas, visual);

            EnsureNodeAnimation(visual, anim);
            visual.X = box.X;
            visual.Y = box.Y;
            visual.Width = box.Width;
            visual.Height = box.Height;
            visual.RemoveOnCompleted = false;

            if (visual is BaseRoundedRectangleGeometry rr)
            {
                var r = (float)NodeCornerRadius;
                rr.BorderRadius = new LvcPoint(r, r);
            }

            MeasureNodeLabel(node, box, chartCenterX, anim, sankeyChart);
            _ = seenNodes.Add(node);
        }

        // Links — use LinkFill if set, else fall back to Fill
        var ribbonPaint = LinkFill is not null && LinkFill != Paint.Default ? LinkFill : Fill;
        var seenLinks = new HashSet<SankeyLink<TNode>>(ReferenceComparer<SankeyLink<TNode>>.Instance);
        foreach (var rb in layout.Links)
        {
            var visual = EnsureVisualForLink(rb.Link, rb);
            if (ribbonPaint is not null && ribbonPaint != Paint.Default)
                ribbonPaint.AddGeometryToPaintTask(sankeyChart.Canvas, visual);

            EnsureRibbonAnimation(visual, anim);
            visual.SourceX = rb.SourceX;
            visual.SourceY0 = rb.SourceY0;
            visual.SourceY1 = rb.SourceY1;
            visual.TargetX = rb.TargetX;
            visual.TargetY0 = rb.TargetY0;
            visual.TargetY1 = rb.TargetY1;
            visual.RemoveOnCompleted = false;

            _ = seenLinks.Add(rb.Link);
        }

        // Reap orphans
        if (_nodeVisuals.Count != seenNodes.Count)
        {
            var toRemove = new List<TNode>();
            foreach (var kv in _nodeVisuals)
                if (!seenNodes.Contains(kv.Key))
                {
                    CollapseRemovedNode(kv.Value);
                    toRemove.Add(kv.Key);
                }
            foreach (var k in toRemove) _ = _nodeVisuals.Remove(k);
        }

        if (_linkVisuals.Count != seenLinks.Count)
        {
            var toRemove = new List<SankeyLink<TNode>>();
            foreach (var kv in _linkVisuals)
                if (!seenLinks.Contains(kv.Key))
                {
                    CollapseRemovedRibbon(kv.Value);
                    toRemove.Add(kv.Key);
                }
            foreach (var k in toRemove) _ = _linkVisuals.Remove(k);
        }

        if (_nodeLabels.Count > 0)
        {
            var toRemove = new List<TNode>();
            foreach (var kv in _nodeLabels)
                if (!seenNodes.Contains(kv.Key))
                {
                    kv.Value.Opacity = 0;
                    kv.Value.RemoveOnCompleted = true;
                    toRemove.Add(kv.Key);
                }
            foreach (var k in toRemove) _ = _nodeLabels.Remove(k);
        }
    }

    /// <summary>
    /// Resolves a node's label, with a fallback to <see cref="SankeyNode.Name"/>
    /// when no <see cref="LabelMapper"/> is set and the node is the built-in type.
    /// </summary>
    private string? ResolveLabel(TNode node)
    {
        if (LabelMapper is not null) return LabelMapper(node);
        if (node is SankeyNode sn) return sn.Name;
        return null;
    }

    /// <summary>
    /// Places a node label outside the node rectangle. The label goes to the
    /// right of nodes left of chart center and to the left of nodes right of
    /// center — keeps labels out of the way of the next column's nodes and
    /// the chart edges.
    /// </summary>
    private void MeasureNodeLabel(
        TNode node, SankeyLayout.NodeBox box, float chartCenterX,
        Animation anim, SankeyChartEngine chart)
    {
        if (!ShowDataLabels || DataLabelsPaint is null || DataLabelsPaint == Paint.Default)
            return;

        var text = ResolveLabel(node);
        if (string.IsNullOrEmpty(text)) return;

        var nodeCenterX = box.X + box.Width * 0.5f;
        var preferRight = nodeCenterX <= chartCenterX;

        // 4px gap between node edge and label so the stroke / fill doesn't
        // crowd the text.
        const float gap = 4f;
        float labelX = preferRight ? box.X + box.Width + gap : box.X - gap;
        var labelY = box.Y + box.Height * 0.5f;

        if (!_nodeLabels.TryGetValue(node, out var label))
        {
            label = new TLabel
            {
                X = labelX,
                Y = labelY,
                HorizontalAlign = preferRight ? Align.Start : Align.End,
                VerticalAlign = Align.Middle,
                MaxWidth = (float)DataLabelsMaxWidth,
            };
            label.Animate(anim, BaseLabelGeometry.XProperty, BaseLabelGeometry.YProperty);
            _nodeLabels[node] = label;
        }

        DataLabelsPaint.AddGeometryToPaintTask(chart.Canvas, label);

        label.Text = text!;
        label.TextSize = (float)DataLabelsSize;
        label.Padding = DataLabelsPadding;
        label.Paint = DataLabelsPaint;
        label.HorizontalAlign = preferRight ? Align.Start : Align.End;
        label.VerticalAlign = Align.Middle;
        label.X = labelX;
        label.Y = labelY;
        label.Opacity = 1;
        label.RemoveOnCompleted = false;
    }

    private static void EnsureNodeAnimation(TVisual visual, Animation anim)
    {
        // Idempotent: re-animating with the same animation is a no-op.
        visual.Animate(anim);
    }

    private static void EnsureRibbonAnimation(BaseSankeyRibbonGeometry visual, Animation anim)
    {
        visual.Animate(anim);
    }

    private static void CollapseRemovedNode(TVisual visual)
    {
        var cx = visual.X + visual.Width * 0.5f;
        var cy = visual.Y + visual.Height * 0.5f;
        visual.X = cx;
        visual.Y = cy;
        visual.Width = 0;
        visual.Height = 0;
        visual.RemoveOnCompleted = true;
    }

    private static void CollapseRemovedRibbon(BaseSankeyRibbonGeometry visual)
    {
        var midY = (visual.SourceY0 + visual.SourceY1) * 0.5f;
        visual.SourceY0 = midY;
        visual.SourceY1 = midY;
        var midTy = (visual.TargetY0 + visual.TargetY1) * 0.5f;
        visual.TargetY0 = midTy;
        visual.TargetY1 = midTy;
        visual.RemoveOnCompleted = true;
    }

    private void ReapAll()
    {
        foreach (var v in _nodeVisuals.Values) CollapseRemovedNode(v);
        _nodeVisuals.Clear();
        foreach (var v in _linkVisuals.Values) CollapseRemovedRibbon(v);
        _linkVisuals.Clear();
        foreach (var l in _nodeLabels.Values)
        {
            l.Opacity = 0;
            l.RemoveOnCompleted = true;
        }
        _nodeLabels.Clear();
    }

    private void InitializePaints(SankeyChartEngine chart)
    {
        var actualZIndex = ZIndex == 0 ? ((ISeries)this).SeriesId : ZIndex;

        // Ribbons draw underneath nodes so the node rectangles sit on top.
        var linkPaint = LinkFill is not null && LinkFill != Paint.Default ? LinkFill : Fill;
        if (linkPaint is not null && linkPaint != Paint.Default)
        {
            linkPaint.ZIndex = actualZIndex + PaintConstants.SeriesFillZIndexOffset;
            chart.Canvas.AddDrawableTask(linkPaint, zone: CanvasZone.DrawMargin);
        }
        if (Fill is not null && Fill != Paint.Default && !ReferenceEquals(Fill, linkPaint))
        {
            Fill.ZIndex = actualZIndex + PaintConstants.SeriesFillZIndexOffset + 1;
            chart.Canvas.AddDrawableTask(Fill, zone: CanvasZone.DrawMargin);
        }
        if (Stroke is not null && Stroke != Paint.Default)
        {
            Stroke.ZIndex = actualZIndex + PaintConstants.SeriesStrokeZIndexOffset;
            chart.Canvas.AddDrawableTask(Stroke, zone: CanvasZone.DrawMargin);
        }
        if (ShowDataLabels && DataLabelsPaint is not null && DataLabelsPaint != Paint.Default)
        {
            // Labels sit above tiles + ribbons; PieSeriesDataLabelsBaseZIndex
            // is the established "always-on-top, no clipping" base offset.
            DataLabelsPaint.ZIndex =
                PaintConstants.PieSeriesDataLabelsBaseZIndex + actualZIndex + PaintConstants.SeriesGeometryFillZIndexOffset;
            chart.Canvas.AddDrawableTask(DataLabelsPaint);
        }
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
    protected internal override Paint?[] GetPaintTasks() => [Fill, Stroke, LinkFill, DataLabelsPaint];

    /// <inheritdoc cref="Series{TModel, TVisual, TLabel}.SetDefaultPointTransitions(ChartPoint)"/>
    protected override void SetDefaultPointTransitions(ChartPoint chartPoint)
    {
        // No-op: sankey visuals are managed directly via _nodeVisuals / _linkVisuals;
        // no ChartPoint round-trip.
    }

    /// <inheritdoc cref="ISeries.SoftDeleteOrDispose(IChartView)"/>
    public override void SoftDeleteOrDispose(IChartView chart)
    {
        var core = ((ISankeyChartView)chart).Core;
        ReapAll();
        foreach (var pt in GetPaintTasks())
            if (pt is not null) core.Canvas.RemovePaintTask(pt);
    }

    private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : notnull
    {
        public static ReferenceComparer<T> Instance { get; } = new();
        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
