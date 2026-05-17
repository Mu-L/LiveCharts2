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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LiveChartsCore.Drawing;
using LiveChartsCore.Geo;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.Painting;

namespace LiveChartsCore;

/// <summary>
/// Defines a geo map chart.
/// </summary>
/// <remarks>
/// PHASE 1 SKETCH: inherits from <see cref="Chart"/> so the map participates in
/// the same lifecycle / throttler / pointer pipeline as Cartesian, Pie, Polar.
///
/// What this class no longer owns (delegated to <see cref="Chart"/> base):
///   - <c>_updateThrottler</c>, <c>_panningThrottler</c> and the
///     <c>Update</c> / <c>UpdateThrottlerUnlocked</c> methods.
///   - <c>_pointerPosition</c>, <c>_isPointerIn</c>, <c>_isPointerDown</c>,
///     <c>_isPanning</c>, <c>_pointerPanningPosition</c>,
///     <c>_pointerPreviousPanningPosition</c> (all base internals).
///   - The custom <c>Action&lt;LvcPoint&gt;</c> pointer events; <see cref="Chart"/>
///     fires its own <c>Action&lt;Chart, LvcPoint&gt;</c> events.
///   - Canvas (via base), the <c>Load</c>/<c>Unload</c> shells.
///
/// What stays geo-specific (overrides):
///   - <see cref="Measure"/>, <see cref="PanningThrottlerUnlocked"/>,
///     <see cref="IsPanEnabled"/>, <see cref="InvokePointerDown"/> family.
///   - Land-based tooltip pipeline (own <c>_tooltipThrottler</c> — different
///     semantics from <see cref="IChartTooltip"/>; unifying is Phase 2 work).
///   - Viewport state (zoom, pan, rotation) and bounce/rotation animators.
/// </remarks>
public class GeoMapChart : Chart
{
    private readonly HashSet<IGeoSeries> _everMeasuredSeries = [];
    private readonly ActionThrottler _tooltipThrottler;
    private bool _isHeatInCanvas = false;
    private Paint _heatPaint;
    private Paint? _previousStroke;
    private Paint? _previousFill;
    private IMapFactory _mapFactory;
    private DrawnMap? _activeMap;
    private bool _isUnloaded = false;
    // _isToolTipOpen is inherited from Chart base; we reuse it for the geo tooltip.
    private LandDefinition? _hoveredLand;
    private float _zoomLevel = 1f;
    private LvcPoint _panOffset = new(0, 0);
    private bool _isBouncing = false;
    private System.Threading.Timer? _bounceTimer;
    private LvcPoint _pointerDownPosition = new(-10, -10);
    private bool _pointerDownIsClick = false;
    private double _rotationX;
    private double _rotationY;
    private System.Threading.Timer? _rotationTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeoMapChart"/> class.
    /// </summary>
    /// <param name="mapView">The map view.</param>
    public GeoMapChart(IGeoMapView mapView)
        : base(mapView.CoreCanvas, mapView, ChartKind.GeoMap)
    {
        MapView = mapView;
        _heatPaint = LiveCharts.DefaultSettings.GetProvider().GetSolidColorPaint();
        _mapFactory = LiveCharts.DefaultSettings.GetProvider().GetDefaultMapFactory();
        _tooltipThrottler = new ActionThrottler(TooltipThrottlerUnlocked, TimeSpan.FromMilliseconds(50));
    }

    /// <summary>
    /// Occurs when a land (country) is clicked.
    /// </summary>
    public event Action<LandClickedEventArgs>? LandClicked;

    /// <summary>
    /// Gets the map view, typed.
    /// </summary>
    public IGeoMapView MapView { get; }

    /// <inheritdoc/>
    public override IChartView View => MapView;

    /// <inheritdoc/>
    public override IEnumerable<ISeries> Series => [];

    /// <inheritdoc/>
    public override IEnumerable<ISeries> VisibleSeries => [];

    /// <inheritdoc/>
    public override IEnumerable<ChartPoint> FindHoveredPointsBy(LvcPoint pointerPosition) => [];

    /// <summary>
    /// Pan is gated by the user's <see cref="IGeoMapView.InteractionMode"/>.
    /// Returning false here makes the base pan-engagement deadzone in
    /// <see cref="Chart.InvokePointerMove"/> a no-op so a drag stays a
    /// tooltip-only gesture instead of moving the map.
    /// </summary>
    internal override bool IsPanEnabled =>
        (MapView.InteractionMode & MapInteractionMode.Pan) == MapInteractionMode.Pan;

    /// <summary>Gets the current zoom level.</summary>
    public float ZoomLevel
    {
        get => _zoomLevel;
        internal set => _zoomLevel = value;
    }

    /// <summary>Gets the current pan offset.</summary>
    public LvcPoint PanOffset
    {
        get => _panOffset;
        internal set => _panOffset = value;
    }

    /// <summary>Rotation center longitude (used for Orthographic projection).</summary>
    public double RotationX
    {
        get => _rotationX;
        set { _rotationX = value; Update(new ChartUpdateParams { Throttling = false }); }
    }

    /// <summary>Rotation center latitude (used for Orthographic projection).</summary>
    public double RotationY
    {
        get => _rotationY;
        set { _rotationY = value; Update(new ChartUpdateParams { Throttling = false }); }
    }

    /// <inheritdoc cref="IMapFactory.ViewTo(GeoMapChart, object)"/>
    public virtual void ViewTo(object? command) => _mapFactory.ViewTo(this, command);

    /// <inheritdoc cref="IMapFactory.Pan(GeoMapChart, LvcPoint)"/>
    public virtual void Pan(LvcPoint delta) => _mapFactory.Pan(this, delta);

    /// <inheritdoc cref="IMapFactory.Zoom(GeoMapChart, LvcPoint, ZoomDirection)"/>
    public virtual void Zoom(LvcPoint pivot, ZoomDirection direction) =>
        _mapFactory.Zoom(this, pivot, direction);

    /// <summary>Resets the viewport to default zoom and pan.</summary>
    public void ResetViewport()
    {
        _isBouncing = false;
        _mapFactory.SetViewport(this, 1f, new LvcPoint(0, 0));
    }

    /// <summary>
    /// Animates the globe rotation. Only visual under <see cref="MapProjection.Orthographic"/>.
    /// </summary>
    public void RotateTo(double longitude, double latitude, int durationMs = 800)
    {
        _rotationTimer?.Dispose();
        _rotationTimer = null;
        AnimateRotation(longitude, latitude, durationMs);
    }

    /// <summary>
    /// Invokes a pointer wheel event. No-op when zoom is disabled via
    /// <see cref="IGeoMapView.InteractionMode"/>.
    /// </summary>
    protected internal void InvokePointerWheel(LvcPoint point, ZoomDirection direction)
    {
        if ((MapView.InteractionMode & MapInteractionMode.Zoom) != MapInteractionMode.Zoom) return;
        Zoom(point, direction);
    }

    /// <inheritdoc/>
    public override void Load()
    {
        // Geo-specific resource (re-)init must run before base.Load() queues the
        // first Update — Measure() will read _heatPaint / _mapFactory.
        if (_isUnloaded)
        {
            _heatPaint = LiveCharts.DefaultSettings.GetProvider().GetSolidColorPaint();
            _mapFactory = LiveCharts.DefaultSettings.GetProvider().GetDefaultMapFactory();
            _isHeatInCanvas = false;
            _isUnloaded = false;
        }
        base.Load();
    }

    /// <inheritdoc/>
    public override void Unload()
    {
        if (_isUnloaded) { base.Unload(); return; }

        if (_isToolTipOpen)
        {
            MapView.Tooltip?.Hide(this);
            _isToolTipOpen = false;
        }
        _hoveredLand = null;

        if (MapView.Stroke is not null) Canvas.RemovePaintTask(MapView.Stroke);
        if (MapView.Fill is not null) Canvas.RemovePaintTask(MapView.Fill);

        _bounceTimer?.Dispose(); _bounceTimer = null;
        _isBouncing = false;
        _rotationTimer?.Dispose(); _rotationTimer = null;

        _everMeasuredSeries.Clear();
        _heatPaint = null!;
        _previousStroke = null!;
        _previousFill = null!;
        _isUnloaded = true;
        _mapFactory.Dispose();

        // Do NOT dispose _activeMap: the same instance is referenced by
        // MapView.ActiveMap and disposing here would make the chart unrenderable
        // on a subsequent Load (issue #1417). The view owns the map's lifetime.
        _activeMap = null!;
        _mapFactory = null!;

        base.Unload();
    }

    /// <inheritdoc/>
    protected internal override void InvokePointerDown(LvcPoint point, bool isSecondaryAction)
    {
        // Track click intent for LandClicked. Base will set _isPointerDown / seed
        // _pointerPosition / etc; we just track the geo-specific bits.
        _pointerDownPosition = point;
        _pointerDownIsClick = true;
        base.InvokePointerDown(point, isSecondaryAction);
    }

    /// <inheritdoc/>
    protected internal override void InvokePointerMove(LvcPoint point)
    {
        // Base handles _pointerPosition, _isPointerIn, pan-engagement deadzone
        // (which uses our IsPanEnabled override) and panning throttler dispatch.
        base.InvokePointerMove(point);

        if (_isPointerDown)
        {
            var dx = point.X - _pointerDownPosition.X;
            var dy = point.Y - _pointerDownPosition.Y;
            if (dx * dx + dy * dy > 25) _pointerDownIsClick = false;
        }

        // Geo tooltip dispatch uses its own throttler with land-based semantics
        // distinct from IChartTooltip. Base's _tooltipThrottler still fires but
        // DrawToolTip exits early because FindHoveredPointsBy returns [].
        if (!_isPanning && !_isPointerDown) _tooltipThrottler.Call();
    }

    /// <inheritdoc/>
    protected internal override void InvokePointerUp(LvcPoint point, bool isSecondaryAction)
    {
        var wasClick = _pointerDownIsClick;
        _pointerDownIsClick = false;

        base.InvokePointerUp(point, isSecondaryAction);

        if (_isPanning is false) BounceBack();

        if (wasClick && LandClicked is not null)
        {
            var result = FindLandAt(point);
            if (result is not null)
            {
                LandClicked.Invoke(new LandClickedEventArgs
                {
                    Land = result.Value.Land,
                    Value = result.Value.Value,
                    Position = point
                });
            }
        }
    }

    /// <inheritdoc/>
    protected internal override void InvokePointerLeft()
    {
        base.InvokePointerLeft();

        if (_isToolTipOpen)
        {
            _hoveredLand = null;
            _isToolTipOpen = false;
            View.InvokeOnUIThread(() =>
            {
                MapView.Tooltip?.Hide(this);
                Canvas.Invalidate();
            });
        }
    }

    /// <inheritdoc/>
    protected override Task PanningThrottlerUnlocked()
    {
        return Task.Run(() =>
            View.InvokeOnUIThread(() =>
            {
                lock (Canvas.Sync)
                {
                    Pan(
                        new LvcPoint(
                            (float)(_pointerPosition.X - _pointerDownPosition.X),
                            (float)(_pointerPosition.Y - _pointerDownPosition.Y)));
                    _pointerDownPosition = _pointerPosition;
                }
            }));
    }

    // TEMPORARY profiling instrumentation for MAUI rotation perf.
    // Aggregates per-section timings over PerfSampleSize measures and writes
    // a single Trace line so MAUI's debug log (Xcode console / logcat / VS
    // Output) gets one summary every ~half-second of rotation. Remove this
    // (and the s_perf* fields) once we have the data.
    private const int PerfSampleSize = 30;
    private static long s_projTicks, s_landsTicks, s_seriesTicks, s_totalTicks;
    private static int s_perfCount;

    /// <inheritdoc/>
    protected internal override void Measure()
    {
        var perfT0 = Stopwatch.GetTimestamp();

        if (_activeMap is not null && _activeMap != MapView.ActiveMap)
        {
            _previousStroke?.ClearGeometriesFromPaintTask(Canvas);
            _previousFill?.ClearGeometriesFromPaintTask(Canvas);

            _previousFill = null;
            _previousStroke = null;

            Canvas.Clear();
        }
        _activeMap = MapView.ActiveMap;

        if (!_isHeatInCanvas)
        {
            Canvas.AddDrawableTask(_heatPaint);
            _isHeatInCanvas = true;
        }

        if (_previousStroke != MapView.Stroke)
        {
            if (_previousStroke is not null) Canvas.RemovePaintTask(_previousStroke);

            if (MapView.Stroke is not null)
            {
                if (MapView.Stroke.ZIndex == 0) MapView.Stroke.ZIndex = PaintConstants.GeoMapStrokeZIndex;
                MapView.Stroke.PaintStyle = PaintStyle.Stroke;
                Canvas.AddDrawableTask(MapView.Stroke);
            }

            _previousStroke = MapView.Stroke;
        }

        if (_previousFill != MapView.Fill)
        {
            if (_previousFill is not null) Canvas.RemovePaintTask(_previousFill);

            if (MapView.Fill is not null)
            {
                MapView.Fill.PaintStyle = PaintStyle.Fill;
                Canvas.AddDrawableTask(MapView.Fill);
            }

            _previousFill = MapView.Fill;
        }

        var i = _previousFill?.ZIndex ?? 0;
        _heatPaint.ZIndex = i + 1;

        var perfProjStart = Stopwatch.GetTimestamp();
        var context = new MapContext(
            this, MapView, MapView.ActiveMap,
            Maps.BuildProjector(
                MapView.MapProjection,
                [MapView.ControlSize.Width, MapView.ControlSize.Height],
                _rotationX, _rotationY));
        var perfProjEnd = Stopwatch.GetTimestamp();
        s_projTicks += perfProjEnd - perfProjStart;

        _mapFactory.GenerateLands(context);
        var perfLandsEnd = Stopwatch.GetTimestamp();
        s_landsTicks += perfLandsEnd - perfProjEnd;

        // Departed series must be deleted BEFORE measuring the new series.
        // Otherwise CoreHeatLandSeries.Delete -> ClearHeat would null the Shape.Fill
        // on lands shared with the new series AFTER the new series painted them,
        // making shared lands appear blank on series swap (issue #962).
        var currentSeries = MapView.Series?.Cast<IGeoSeries>().ToArray() ?? [];
        var currentSet = new HashSet<IGeoSeries>(currentSeries);
        foreach (var series in _everMeasuredSeries)
        {
            if (currentSet.Contains(series)) continue;
            series.Delete(context);
        }
        _everMeasuredSeries.RemoveWhere(s => !currentSet.Contains(s));

        foreach (var series in currentSeries)
        {
            series.Measure(context);
            _ = _everMeasuredSeries.Add(series);
        }
        s_seriesTicks += Stopwatch.GetTimestamp() - perfLandsEnd;

        if (_hoveredLand is not null && MapView.Tooltip is not null &&
            MapView.TooltipPosition != TooltipPosition.Hidden)
        {
            var value = 0d;
            var hasValue = false;
            foreach (var series in MapView.Series?.Cast<IGeoSeries>() ?? [])
            {
                if (series.TryGetValue(_hoveredLand.ShortName, out value))
                { hasValue = true; break; }
            }

            var center = ComputeLandScreenCenter(_hoveredLand, context.Projector);

            MapView.Tooltip.Show(
                new GeoTooltipPoint
                {
                    Land = _hoveredLand,
                    Value = value,
                    HasValue = hasValue,
                    LandCenter = center
                },
                this);
        }

        Canvas.Invalidate();

        s_totalTicks += Stopwatch.GetTimestamp() - perfT0;
        if (++s_perfCount >= PerfSampleSize)
        {
            var perFreq = 1000.0 / Stopwatch.Frequency;
            Trace.WriteLine(
                $"[MAP-PERF] n={s_perfCount} avg ms — " +
                $"total={s_totalTicks * perFreq / s_perfCount:F2} " +
                $"projector={s_projTicks * perFreq / s_perfCount:F2} " +
                $"lands={s_landsTicks * perFreq / s_perfCount:F2} " +
                $"series={s_seriesTicks * perFreq / s_perfCount:F2}");
            s_totalTicks = s_projTicks = s_landsTicks = s_seriesTicks = 0;
            s_perfCount = 0;
        }
    }

    /// <summary>
    /// Finds the land definition at the specified pointer position, if any.
    /// </summary>
    public (LandDefinition Land, double Value, bool HasValue, LvcPoint Center)? FindLandAt(LvcPoint pointerPosition)
    {
        if (_activeMap is null) return null;

        foreach (var layer in _activeMap.Layers.Values)
        {
            if (!layer.IsVisible) continue;

            foreach (var landDefinition in layer.Lands.Values)
            {
                foreach (var landData in landDefinition.Data)
                {
                    if (landData.Shape is null) continue;
                    if (!landData.Shape.ContainsPoint(pointerPosition.X, pointerPosition.Y)) continue;

                    var value = 0d;
                    var hasValue = false;
                    foreach (var series in MapView.Series?.Cast<IGeoSeries>() ?? [])
                    {
                        if (series.TryGetValue(landDefinition.ShortName, out value))
                        { hasValue = true; break; }
                    }

                    var projector = Maps.BuildProjector(
                        MapView.MapProjection,
                        [MapView.ControlSize.Width, MapView.ControlSize.Height],
                        _rotationX, _rotationY);
                    var center = ComputeLandScreenCenter(landDefinition, projector);

                    return (landDefinition, value, hasValue, center);
                }
            }
        }

        return null;
    }

    private LvcPoint ComputeLandScreenCenter(LandDefinition land, MapProjector projector)
    {
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        var hasPoints = false;

        foreach (var data in land.Data)
        {
            foreach (var coord in data.Coordinates)
            {
                if (!projector.IsVisible(coord.X, coord.Y)) continue;
                var projected = projector.ToMap([coord.X, coord.Y]);
                var px = projected[0];
                var py = projected[1];
                if (px < minX) minX = px;
                if (py < minY) minY = py;
                if (px > maxX) maxX = px;
                if (py > maxY) maxY = py;
                hasPoints = true;
            }
        }

        if (!hasPoints) return _pointerPosition;

        var baseCx = (minX + maxX) / 2f;
        var baseCy = (minY + maxY) / 2f;
        var ctrlCx = MapView.ControlSize.Width * 0.5f;
        var ctrlCy = MapView.ControlSize.Height * 0.5f;
        var tx = ctrlCx * (1 - _zoomLevel) + _panOffset.X;
        var ty = ctrlCy * (1 - _zoomLevel) + _panOffset.Y;
        return new LvcPoint(baseCx * _zoomLevel + tx, baseCy * _zoomLevel + ty);
    }

    private Task TooltipThrottlerUnlocked()
    {
        return Task.Run(() =>
            View.InvokeOnUIThread(() =>
            {
                lock (Canvas.Sync)
                {
                    if (_isUnloaded || _isPanning || !_isPointerIn) return;
                    if (MapView.Tooltip is null || MapView.TooltipPosition == TooltipPosition.Hidden) return;

                    var result = FindLandAt(_pointerPosition);

                    if (result is null)
                    {
                        if (_isToolTipOpen)
                        {
                            _hoveredLand = null;
                            _isToolTipOpen = false;
                            MapView.Tooltip.Hide(this);
                            Canvas.Invalidate();
                        }
                        return;
                    }

                    var (land, value, hasValue, center) = result.Value;

                    if (land == _hoveredLand) return;
                    _hoveredLand = land;
                    _isToolTipOpen = true;

                    MapView.Tooltip.Show(
                        new GeoTooltipPoint
                        {
                            Land = land,
                            Value = value,
                            HasValue = hasValue,
                            LandCenter = center
                        },
                        this);
                    Canvas.Invalidate();
                }
            }));
    }

    private void BounceBack()
    {
        if (_isBouncing) return;

        var controlW = MapView.ControlSize.Width;
        var controlH = MapView.ControlSize.Height;
        if (controlW <= 0 || controlH <= 0) return;

        var cx = controlW * 0.5f;
        var cy = controlH * 0.5f;
        var zoom = _zoomLevel;
        var minZoom = (float)MapView.MinZoomLevel;
        var targetZoom = zoom < minZoom ? minZoom : zoom;

        var mapScreenW = controlW * targetZoom;
        var mapScreenH = controlH * targetZoom;

        var targetPanX = _panOffset.X;
        var targetPanY = _panOffset.Y;

        if (Math.Abs(targetZoom - zoom) > 1e-6)
        {
            targetPanX = _panOffset.X * targetZoom / zoom;
            targetPanY = _panOffset.Y * targetZoom / zoom;
        }

        var tx = cx * (1 - targetZoom) + targetPanX;
        var ty = cy * (1 - targetZoom) + targetPanY;

        if (tx > 0) tx = 0;
        if (tx + mapScreenW < controlW) tx = controlW - mapScreenW;
        if (ty > 0) ty = 0;
        if (ty + mapScreenH < controlH) ty = controlH - mapScreenH;

        targetPanX = tx - cx * (1 - targetZoom);
        targetPanY = ty - cy * (1 - targetZoom);

        var panDiffX = Math.Abs(targetPanX - _panOffset.X);
        var panDiffY = Math.Abs(targetPanY - _panOffset.Y);
        var zoomDiff = Math.Abs(targetZoom - zoom);

        if (panDiffX < 0.5f && panDiffY < 0.5f && zoomDiff < 0.001f) return;

        _isBouncing = true;
        AnimateBounce(targetPanX, targetPanY, targetZoom);
    }

    private void AnimateBounce(float targetPanX, float targetPanY, float targetZoom)
    {
        const int steps = 8;
        const int intervalMs = 16;
        var step = 0;

        var startPanX = _panOffset.X;
        var startPanY = _panOffset.Y;
        var startZoom = _zoomLevel;

        _bounceTimer?.Dispose();
        _bounceTimer = new System.Threading.Timer(_ =>
        {
            if (_isUnloaded)
            {
                _isBouncing = false;
                _bounceTimer?.Dispose();
                _bounceTimer = null;
                return;
            }

            step++;
            var t = (float)step / steps;
            t = 1 - (1 - t) * (1 - t) * (1 - t);

            var newPanX = startPanX + (targetPanX - startPanX) * t;
            var newPanY = startPanY + (targetPanY - startPanY) * t;
            var newZoom = startZoom + (targetZoom - startZoom) * t;

            View.InvokeOnUIThread(() =>
            {
                if (_isUnloaded) return;
                _mapFactory.SetViewport(this, newZoom, new LvcPoint(newPanX, newPanY));
            });

            if (step >= steps)
            {
                _isBouncing = false;
                _bounceTimer?.Dispose();
                _bounceTimer = null;
            }
        }, null, intervalMs, intervalMs);
    }

    private void AnimateRotation(double targetLon, double targetLat, int durationMs)
    {
        const int intervalMs = 16;
        var totalSteps = Math.Max(1, durationMs / intervalMs);
        var step = 0;

        var startLon = _rotationX;
        var startLat = _rotationY;

        var deltaLon = targetLon - startLon;
        if (deltaLon > 180) deltaLon -= 360;
        if (deltaLon < -180) deltaLon += 360;

        _rotationTimer = new System.Threading.Timer(_ =>
        {
            if (_isUnloaded)
            {
                _rotationTimer?.Dispose();
                _rotationTimer = null;
                return;
            }

            step++;
            var t = (float)step / totalSteps;

            t = t < 0.5f
                ? 4 * t * t * t
                : 1 - (float)Math.Pow(-2 * t + 2, 3) / 2;

            _rotationX = startLon + deltaLon * t;
            _rotationY = startLat + (targetLat - startLat) * t;

            View.InvokeOnUIThread(() =>
            {
                if (_isUnloaded) return;
                lock (Canvas.Sync) { Measure(); }
            });

            if (step >= totalSteps)
            {
                _rotationX = targetLon;
                _rotationY = targetLat;
                _rotationTimer?.Dispose();
                _rotationTimer = null;
            }
        }, null, intervalMs, intervalMs);
    }
}
