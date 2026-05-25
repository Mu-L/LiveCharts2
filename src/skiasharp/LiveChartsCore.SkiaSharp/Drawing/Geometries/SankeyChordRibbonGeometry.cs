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

using LiveChartsCore.Drawing;
using SkiaSharp;

namespace LiveChartsCore.SkiaSharpView.Drawing.Geometries;

/// <summary>
/// SkiaSharp implementation of a sankey chord ribbon (used in BipartiteArc
/// layout). Draws a closed band from a source-arc chord to a target-arc chord,
/// with both cubic curves' control points coincident at
/// (<see cref="BaseSankeyChordRibbonGeometry.CenterX"/>,
/// <see cref="BaseSankeyChordRibbonGeometry.CenterY"/>) — this is the
/// d3-chord convention: control-point-at-center collapses the cubic to a
/// quadratic through the center, producing the chord-diagram "bowtie" curve.
/// </summary>
public class SankeyChordRibbonGeometry : BaseSankeyChordRibbonGeometry, IDrawnElement<SkiaSharpDrawingContext>
{
    private SKPath? _cachedPath;

    /// <inheritdoc cref="IDrawnElement{TDrawingContext}.Draw(TDrawingContext)" />
    public void Draw(SkiaSharpDrawingContext context)
    {
        var cx = CenterX;
        var cy = CenterY;

        var path = _cachedPath ??= new SKPath();
        path.Reset();

        // s0 → t1 (top edge, curving through center)
        // t1 → t0 (target chord)
        // t0 → s1 (bottom edge, curving back through center)
        // s1 → s0 (source chord, implicit Close)
        path.MoveTo(SourceP0X, SourceP0Y);
        path.CubicTo(cx, cy, cx, cy, TargetP1X, TargetP1Y);
        path.LineTo(TargetP0X, TargetP0Y);
        path.CubicTo(cx, cy, cx, cy, SourceP1X, SourceP1Y);
        path.Close();

        var c = Color;
        var activePaint = context.ActiveSkiaPaint;
        if (!c.Equals(LvcColor.Empty))
            activePaint.Color = new SKColor(c.R, c.G, c.B, c.A);

        context.Canvas.DrawPath(path, activePaint);
    }

    /// <inheritdoc cref="DrawnGeometry.Measure()" />
    public override LvcSize Measure()
    {
        // Loose bounds: span across all 4 anchors. Sufficient for paint-task
        // dispatch; no fine-grained hit-testing consumes Measure() here yet.
        var minX = SourceP0X; var maxX = SourceP0X;
        if (SourceP1X < minX) minX = SourceP1X; if (SourceP1X > maxX) maxX = SourceP1X;
        if (TargetP0X < minX) minX = TargetP0X; if (TargetP0X > maxX) maxX = TargetP0X;
        if (TargetP1X < minX) minX = TargetP1X; if (TargetP1X > maxX) maxX = TargetP1X;
        var minY = SourceP0Y; var maxY = SourceP0Y;
        if (SourceP1Y < minY) minY = SourceP1Y; if (SourceP1Y > maxY) maxY = SourceP1Y;
        if (TargetP0Y < minY) minY = TargetP0Y; if (TargetP0Y > maxY) maxY = TargetP0Y;
        if (TargetP1Y < minY) minY = TargetP1Y; if (TargetP1Y > maxY) maxY = TargetP1Y;
        return new LvcSize(maxX - minX, maxY - minY);
    }
}
