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
using LiveChartsCore.Drawing;
using SkiaSharp;

namespace LiveChartsCore.SkiaSharpView.Drawing.Geometries;

/// <summary>
/// SkiaSharp implementation of a sankey arc-segment node tile. Draws an
/// annular sector (donut wedge) between InnerRadius and OuterRadius across
/// [StartAngle, StartAngle + SweepAngle] (degrees, 0° = east, clockwise),
/// optionally with rounded radial edges.
/// </summary>
public class SankeyArcSegmentGeometry : BaseSankeyArcSegmentGeometry, IDrawnElement<SkiaSharpDrawingContext>
{
    private SKPath? _cachedPath;

    /// <inheritdoc cref="IDrawnElement{TDrawingContext}.Draw(TDrawingContext)" />
    public void Draw(SkiaSharpDrawingContext context)
    {
        var sweep = SweepAngle;
        if (sweep <= 0f) return;

        var cx = CenterX;
        var cy = CenterY;
        var inner = InnerRadius;
        var outer = OuterRadius;
        var start = StartAngle;
        const float toRadians = (float)(Math.PI / 180);

        var path = _cachedPath ??= new SKPath();
        path.Reset();

        // 4-corner annular sector: walk inner-start → outer-start (radial),
        // outer arc forward, outer-end → inner-end (radial), inner arc reverse.
        var outerRect = new SKRect(cx - outer, cy - outer, cx + outer, cy + outer);
        var innerRect = new SKRect(cx - inner, cy - inner, cx + inner, cy + inner);

        path.MoveTo(
            (float)(cx + Math.Cos(start * toRadians) * inner),
            (float)(cy + Math.Sin(start * toRadians) * inner));
        path.LineTo(
            (float)(cx + Math.Cos(start * toRadians) * outer),
            (float)(cy + Math.Sin(start * toRadians) * outer));
        path.ArcTo(outerRect, start, sweep, false);
        path.LineTo(
            (float)(cx + Math.Cos((start + sweep) * toRadians) * inner),
            (float)(cy + Math.Sin((start + sweep) * toRadians) * inner));
        path.ArcTo(innerRect, start + sweep, -sweep, false);
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
        // Bounding square of the outer circle is a generous upper bound; arc
        // segments never extend beyond it. Width=Height keeps the layout code
        // simple — no consumer of SankeyArcSegmentGeometry uses Measure() for
        // tight bounds today, so the loose bound is fine.
        var d = OuterRadius * 2f;
        return new LvcSize(d, d);
    }
}
