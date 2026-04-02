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
using LiveChartsCore.Drawing.Segments;
using SkiaSharp;

namespace LiveChartsCore.SkiaSharpView.Drawing.Geometries;

/// <summary>
/// Holds shared viewport transform state for all land geometries.
/// A single instance is shared across all geometries — update once, affects all draws.
/// </summary>
public class MapViewportTransform
{
    /// <summary>
    /// Gets or sets the zoom level.
    /// </summary>
    public float Zoom { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the pan X offset.
    /// </summary>
    public float PanX { get; set; }

    /// <summary>
    /// Gets or sets the pan Y offset.
    /// </summary>
    public float PanY { get; set; }

    /// <summary>
    /// Gets or sets the center X of the control.
    /// </summary>
    public float CenterX { get; set; }

    /// <summary>
    /// Gets or sets the center Y of the control.
    /// </summary>
    public float CenterY { get; set; }

    /// <summary>
    /// Gets whether this transform is non-identity.
    /// </summary>
    public bool IsActive => Zoom != 1f || PanX != 0 || PanY != 0;

    /// <summary>
    /// Builds the SKMatrix for the current viewport transform.
    /// </summary>
    public SKMatrix GetMatrix()
    {
        var tx = CenterX * (1 - Zoom) + PanX;
        var ty = CenterY * (1 - Zoom) + PanY;
        return new SKMatrix(Zoom, 0, tx, 0, Zoom, ty, 0, 0, 1);
    }

    /// <summary>
    /// Inverse-transforms a screen point to base (unzoomed) coordinates.
    /// </summary>
    public (float x, float y) InverseTransform(float screenX, float screenY)
    {
        var tx = CenterX * (1 - Zoom) + PanX;
        var ty = CenterY * (1 - Zoom) + PanY;
        return ((screenX - tx) / Zoom, (screenY - ty) / Zoom);
    }
}

/// <summary>
/// Defines a land area geometry with cached path rendering for smooth zoom/pan.
/// Re-implements the draw interface to bypass per-frame path rebuilding.
/// Uses canvas matrix transform (GPU-accelerated) — zero allocations during zoom/pan.
/// </summary>
public class LandAreaGeometry : VectorGeometry, IDrawnElement<SkiaSharpDrawingContext>
{
    private SKPath? _basePath;
    private bool _pathDirty = true;

    /// <summary>
    /// Gets or sets the shared viewport transform applied during rendering.
    /// </summary>
    public MapViewportTransform? ViewportTransform { get; set; }

    /// <summary>
    /// Marks the cached path as dirty, forcing a rebuild on the next draw.
    /// Call this after modifying the Commands collection.
    /// </summary>
    public void MarkPathDirty()
    {
        _pathDirty = true;
    }

    /// <summary>
    /// Sets a pre-built base path directly, bypassing the Commands-based path building.
    /// Used for combined geometries where multiple sub-paths need proper MoveTo calls.
    /// </summary>
    /// <param name="path">The pre-built SKPath. Ownership is transferred to this geometry.</param>
    public void SetBasePath(SKPath path)
    {
        _basePath?.Dispose();
        _basePath = path;
        _pathDirty = false;
    }

    /// <summary>
    /// Draws the land area using a cached base path with GPU canvas matrix transform.
    /// Zero allocations during zoom/pan — just Save/Concat/DrawPath/Restore.
    /// </summary>
    void IDrawnElement<SkiaSharpDrawingContext>.Draw(SkiaSharpDrawingContext context)
    {
        if (_basePath is null && Commands.Count == 0) return;

        // Rebuild base path only when geometry data changes (map load, resize)
        if (_pathDirty || _basePath is null)
        {
            _basePath?.Dispose();
            _basePath = new SKPath();

            var isFirst = true;
            foreach (var segment in Commands)
            {
                if (isFirst)
                {
                    _basePath.MoveTo(segment.Xi, segment.Yi);
                    isFirst = false;
                }
                _basePath.LineTo(segment.Xi, segment.Yi);
            }

            _pathDirty = false;
        }

        // Apply viewport transform via canvas matrix (GPU) and draw cached path
        var vt = ViewportTransform;
        if (vt is not null && vt.IsActive)
        {
            var state = context.Canvas.Save();
            var matrix = vt.GetMatrix();
            context.Canvas.Concat(ref matrix);
            context.Canvas.DrawPath(_basePath, context.ActiveSkiaPaint);
            context.Canvas.RestoreToCount(state);
        }
        else
        {
            context.Canvas.DrawPath(_basePath, context.ActiveSkiaPaint);
        }
    }

    /// <inheritdoc cref="VectorGeometry.OnDrawSegment(SkiaSharpDrawingContext, SKPath, Segment)"/>
    protected override void OnDrawSegment(SkiaSharpDrawingContext context, SKPath path, Segment segment) =>
        path.LineTo(segment.Xi, segment.Yi);

    /// <inheritdoc cref="VectorGeometry.OnOpen(SkiaSharpDrawingContext, SKPath, Segment)"/>
    protected override void OnOpen(SkiaSharpDrawingContext context, SKPath path, Segment segment) =>
        path.MoveTo(segment.Xi, segment.Yi);

    /// <inheritdoc cref="VectorGeometry.OnClose(SkiaSharpDrawingContext, SKPath, Segment)"/>
    protected override void OnClose(SkiaSharpDrawingContext context, SKPath path, Segment segment)
    { }

    /// <summary>
    /// Determines whether the specified point is inside this land polygon.
    /// </summary>
    public override bool ContainsPoint(float x, float y)
    {
        var vt = ViewportTransform;
        if (vt is not null && vt.IsActive)
            (x, y) = vt.InverseTransform(x, y);

        if (_basePath is not null && !_pathDirty)
        {
            using var closedPath = new SKPath(_basePath);
            closedPath.Close();
            return closedPath.Contains(x, y);
        }

        if (Commands.Count == 0) return false;

        using var path = new SKPath();
        var isFirst = true;

        foreach (var segment in Commands)
        {
            if (isFirst)
            {
                path.MoveTo(segment.Xi, segment.Yi);
                isFirst = false;
            }
            path.LineTo(segment.Xi, segment.Yi);
        }

        path.Close();
        return path.Contains(x, y);
    }
}
