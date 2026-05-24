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

    /// <inheritdoc cref="ISankeySeries.NodeLabelPosition"/>
    public SankeyLabelPosition NodeLabelPosition { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Per-node color override. When non-null, the returned <see cref="LvcColor"/>
    /// is applied to the node's visual — the visual must implement
    /// <see cref="IColoredGeometry"/> (the default
    /// <c>ColoredRoundedRectangleGeometry</c> does). When null the series's
    /// <see cref="Fill"/> paint color flows through unchanged.
    /// </summary>
    public Func<TNode, LvcColor>? NodeColorMapper { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Per-link color override. When null, ribbons inherit the source node's
    /// color (via <see cref="NodeColorMapper"/>) tinted to half opacity, or
    /// fall back to <see cref="LinkFill"/> / <see cref="Fill"/>. Set
    /// explicitly to take full control of per-ribbon color including alpha.
    /// </summary>
    public Func<SankeyLink<TNode>, LvcColor>? LinkColorMapper { get; set => SetProperty(ref field, value); }

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

        // Seed Color BEFORE Animate (in the Invalidate loop below) so the
        // motion property's start value is the right color — assigning after
        // Animate makes the property animate from Empty/Black to the target,
        // which under IsTesting reads as the start (= invisible/wrong color).
        // Same pattern CoreHeatSeries.EnsureVisualForPoint uses.
        if (NodeColorMapper is not null && v is IColoredGeometry coloredInit)
            coloredInit.Color = NodeColorMapper(node);

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

        // Seed Color before Animate — see EnsureVisualForNode rationale.
        if (LinkColorMapper is not null)
        {
            v.Color = LinkColorMapper(link);
        }
        else if (NodeColorMapper is not null)
        {
            var srcColor = NodeColorMapper(link.Source);
            v.Color = new LvcColor(srcColor.R, srcColor.G, srcColor.B, (byte)(srcColor.A / 2));
        }

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

        // For Auto label placement we need per-node in/out degree (source =
        // no incoming -> outside-left; sink = no outgoing -> outside-right;
        // pass-through -> inside). Compute once per measure.
        var incoming = new Dictionary<TNode, int>(ReferenceComparer<TNode>.Instance);
        var outgoing = new Dictionary<TNode, int>(ReferenceComparer<TNode>.Instance);
        foreach (var n in Values) { incoming[n] = 0; outgoing[n] = 0; }
        if (Links is not null)
            foreach (var l in Links)
            {
                if (l is null) continue;
                if (outgoing.ContainsKey(l.Source)) outgoing[l.Source]++;
                if (incoming.ContainsKey(l.Target)) incoming[l.Target]++;
            }

        // Reserve canvas margin for outside-placed labels so they don't clip
        // off the chart edges. Skip for Inside placement and when labels are
        // disabled. Measured up-front so the layout columns get the right
        // horizontal budget on the first try.
        var (leftReserve, rightReserve) = MeasureOutsideLabelReserve(incoming, outgoing);
        var rect = new LvcRectangle(
            new LvcPoint(ctx.DrawLocation.X + leftReserve, ctx.DrawLocation.Y),
            new LvcSize(ctx.DrawMarginSize.Width - leftReserve - rightReserve, ctx.DrawMarginSize.Height));

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

            // Per-node color (requires an IColoredGeometry visual — true for
            // the default ColoredRoundedRectangleGeometry).
            if (NodeColorMapper is not null && visual is IColoredGeometry coloredNode)
                coloredNode.Color = NodeColorMapper(node);

            MeasureNodeLabel(node, box, incoming[node], outgoing[node], chartCenterX, anim, sankeyChart);
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

            // Per-link color: explicit LinkColorMapper wins; otherwise tint
            // the source-node color at half alpha so ribbons read as "from
            // node X" without overpowering the destination.
            if (LinkColorMapper is not null)
            {
                visual.Color = LinkColorMapper(rb.Link);
            }
            else if (NodeColorMapper is not null)
            {
                var srcColor = NodeColorMapper(rb.Link.Source);
                visual.Color = new LvcColor(srcColor.R, srcColor.G, srcColor.B, (byte)(srcColor.A / 2));
            }
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
    /// Places a node label per <see cref="NodeLabelPosition"/>:
    /// <list type="bullet">
    ///   <item><c>Auto</c> — pure source (no incoming) goes outside-left,
    ///     pure sink (no outgoing) goes outside-right, pass-through goes
    ///     inside-centered. Matches the d3-sankey convention.</item>
    ///   <item><c>Outside</c> — flips per-node based on chart center
    ///     (label on the side closer to the chart edge).</item>
    ///   <item><c>Inside</c> — always centered on the node.</item>
    /// </list>
    /// </summary>
    private void MeasureNodeLabel(
        TNode node, SankeyLayout.NodeBox box, int inDeg, int outDeg,
        float chartCenterX, Animation anim, SankeyChartEngine chart)
    {
        if (!ShowDataLabels || DataLabelsPaint is null || DataLabelsPaint == Paint.Default)
            return;

        var text = ResolveLabel(node);
        if (string.IsNullOrEmpty(text)) return;

        // Resolve effective placement.
        var placement = NodeLabelPosition;
        var nodeCenterX = box.X + box.Width * 0.5f;
        bool isInside;
        bool labelOnRight;
        if (placement == SankeyLabelPosition.Inside)
        {
            isInside = true;
            labelOnRight = false; // unused for inside
        }
        else if (placement == SankeyLabelPosition.Outside)
        {
            // The original heuristic: label opposite chart center to avoid the
            // next column / chart edge. Stays meaningful when the user
            // explicitly opts out of Auto.
            isInside = false;
            labelOnRight = nodeCenterX > chartCenterX;
        }
        else
        {
            // Auto: in-deg=0 -> source -> outside-left; out-deg=0 -> sink
            // -> outside-right; both nonzero -> inside.
            if (inDeg == 0 && outDeg > 0) { isInside = false; labelOnRight = false; }
            else if (outDeg == 0 && inDeg > 0) { isInside = false; labelOnRight = true; }
            else { isInside = true; labelOnRight = false; }
        }

        const float gap = 4f;
        float labelX;
        float labelY = box.Y + box.Height * 0.5f;
        Align hAlign;
        if (isInside)
        {
            labelX = box.X + box.Width * 0.5f;
            hAlign = Align.Middle;
        }
        else if (labelOnRight)
        {
            labelX = box.X + box.Width + gap;
            hAlign = Align.Start;
        }
        else
        {
            labelX = box.X - gap;
            hAlign = Align.End;
        }

        if (!_nodeLabels.TryGetValue(node, out var label))
        {
            label = new TLabel
            {
                X = labelX,
                Y = labelY,
                HorizontalAlign = hAlign,
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
        label.HorizontalAlign = hAlign;
        label.VerticalAlign = Align.Middle;
        label.X = labelX;
        label.Y = labelY;
        label.Opacity = 1;
        label.RemoveOnCompleted = false;
    }

    /// <summary>
    /// Returns (leftReserve, rightReserve) — pixel budgets to subtract from
    /// the chart's left/right edges so outside-placed labels fit. Empty when
    /// placement is <see cref="SankeyLabelPosition.Inside"/> or labels are off.
    /// Auto reserves left for pure-source nodes (inDeg=0) and right for
    /// pure-sink nodes (outDeg=0); Outside reserves both sides for all nodes.
    /// </summary>
    private (float Left, float Right) MeasureOutsideLabelReserve(
        Dictionary<TNode, int> incoming, Dictionary<TNode, int> outgoing)
    {
        if (!ShowDataLabels || DataLabelsPaint is null || DataLabelsPaint == Paint.Default)
            return (0f, 0f);
        if (NodeLabelPosition == SankeyLabelPosition.Inside)
            return (0f, 0f);
        if (Values is null) return (0f, 0f);

        const float gap = 4f;

        // A single throwaway label measures successive node texts — Measure()
        // re-renders with the current Text/TextSize/Padding/Paint, so the
        // instance is reusable across iterations.
        var probe = new TLabel
        {
            TextSize = (float)DataLabelsSize,
            Padding = DataLabelsPadding,
            Paint = DataLabelsPaint,
            MaxWidth = (float)DataLabelsMaxWidth,
        };

        float leftMax = 0f, rightMax = 0f;
        foreach (var node in Values)
        {
            var text = ResolveLabel(node);
            if (string.IsNullOrEmpty(text)) continue;

            probe.Text = text!;
            var size = probe.Measure();

            // Outside places every node's label outside (per the heuristic);
            // Auto only places pure-source/pure-sink outside.
            var inDeg = incoming.TryGetValue(node, out var i) ? i : 0;
            var outDeg = outgoing.TryGetValue(node, out var o) ? o : 0;

            if (NodeLabelPosition == SankeyLabelPosition.Outside)
            {
                // Outside flips per side; reserve symmetrically since either
                // edge may host any node's label.
                if (size.Width > leftMax) leftMax = size.Width;
                if (size.Width > rightMax) rightMax = size.Width;
            }
            else // Auto
            {
                if (inDeg == 0 && outDeg > 0 && size.Width > leftMax) leftMax = size.Width;
                else if (outDeg == 0 && inDeg > 0 && size.Width > rightMax) rightMax = size.Width;
            }
        }

        return (leftMax > 0 ? leftMax + gap : 0f, rightMax > 0 ? rightMax + gap : 0f);
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
