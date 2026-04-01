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
using System.Linq;
using System.Threading.Tasks;
using LiveChartsCore.Drawing;
using LiveChartsCore.Geo;
using LiveChartsCore.Kernel;
using LiveChartsCore.Measure;
using LiveChartsCore.Painting;
using LiveChartsCore.Themes;

namespace LiveChartsCore;

/// <summary>
/// Defines a geo map chart.
/// </summary>
public class GeoMapChart
{
    private readonly HashSet<IGeoSeries> _everMeasuredSeries = [];
    private readonly ActionThrottler _updateThrottler;
    private readonly ActionThrottler _panningThrottler;
    private readonly ActionThrottler _tooltipThrottler;
    private bool _isHeatInCanvas = false;
    private Paint _heatPaint;
    private Paint? _previousStroke;
    private Paint? _previousFill;
    private LvcPoint _pointerPanningPosition = new(-10, -10);
    private LvcPoint _pointerPreviousPanningPosition = new(-10, -10);
    private LvcPoint _pointerPosition = new(-10, -10);
    private bool _isPanning = false;
    private bool _isPointerIn = false;
    private IMapFactory _mapFactory;
    private DrawnMap? _activeMap;
    private bool _isUnloaded = false;
    private bool _isToolTipOpen = false;
    private LandDefinition? _hoveredLand;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeoMapChart"/> class.
    /// </summary>
    /// <param name="mapView"></param>
    public GeoMapChart(IGeoMapView mapView)
    {
        View = mapView;
        _updateThrottler = mapView.DesignerMode
                ? new ActionThrottler(() => Task.CompletedTask, TimeSpan.FromMilliseconds(50))
                : new ActionThrottler(UpdateThrottlerUnlocked, TimeSpan.FromMilliseconds(100));
        _heatPaint = LiveCharts.DefaultSettings.GetProvider().GetSolidColorPaint();
        _mapFactory = LiveCharts.DefaultSettings.GetProvider().GetDefaultMapFactory();

        PointerDown += Chart_PointerDown;
        PointerMove += Chart_PointerMove;
        PointerUp += Chart_PointerUp;
        PointerLeft += Chart_PointerLeft;

        _panningThrottler = new ActionThrottler(PanningThrottlerUnlocked, TimeSpan.FromMilliseconds(30));
        _tooltipThrottler = new ActionThrottler(TooltipThrottlerUnlocked, TimeSpan.FromMilliseconds(50));
    }

    internal event Action<LvcPoint> PointerDown;
    internal event Action<LvcPoint> PointerMove;
    internal event Action<LvcPoint> PointerUp;
    internal event Action PointerLeft;

    /// <summary>
    /// Gets the chart view.
    /// </summary>
    public IGeoMapView View { get; private set; }

    /// <summary>
    /// Gets the active theme, ensuring it is set up for the current dark/light mode.
    /// </summary>
    public Theme GetTheme()
    {
        var theme = LiveCharts.DefaultSettings.GetTheme();
        theme.Setup(View.IsDarkMode);
        return theme;
    }

    /// <inheritdoc cref="IMapFactory.ViewTo(GeoMapChart, object)"/>
    public virtual void ViewTo(object? command) => _mapFactory.ViewTo(this, command);

    /// <inheritdoc cref="IMapFactory.Pan(GeoMapChart, LvcPoint)"/>
    public virtual void Pan(LvcPoint delta) => _mapFactory.Pan(this, delta);

    /// <summary>
    /// Queues a measure request to update the chart.
    /// </summary>
    /// <param name="chartUpdateParams"></param>
    public virtual void Update(ChartUpdateParams? chartUpdateParams = null)
    {
        chartUpdateParams ??= new ChartUpdateParams();

        if (chartUpdateParams.IsAutomaticUpdate && !View.AutoUpdateEnabled) return;

        if (!chartUpdateParams.Throttling)
        {
            _updateThrottler.ForceCall();
            return;
        }

        _updateThrottler.Call();
    }

    /// <summary>
    /// Unload the map resources.
    /// </summary>
    public void Unload()
    {
        if (View.Stroke is not null) View.CoreCanvas.RemovePaintTask(View.Stroke);
        if (View.Fill is not null) View.CoreCanvas.RemovePaintTask(View.Fill);

        _everMeasuredSeries.Clear();
        _heatPaint = null!;
        _previousStroke = null!;
        _previousFill = null!;
        _isUnloaded = true;
        _mapFactory.Dispose();
        _activeMap?.Dispose();

        _activeMap = null!;
        _mapFactory = null!;

        View.CoreCanvas.Dispose();
    }

    /// <summary>
    /// Invokes the pointer down event.
    /// </summary>
    /// <param name="point">The pointer position.</param>
    protected internal void InvokePointerDown(LvcPoint point) => PointerDown?.Invoke(point);

    /// <summary>
    /// Invokes the pointer move event.
    /// </summary>
    /// <param name="point">The pointer position.</param>
    protected internal void InvokePointerMove(LvcPoint point) => PointerMove?.Invoke(point);

    /// <summary>
    /// Invokes the pointer up event.
    /// </summary>
    /// <param name="point">The pointer position.</param>
    protected internal void InvokePointerUp(LvcPoint point) => PointerUp?.Invoke(point);

    /// <summary>
    /// Invokes the pointer left event.
    /// </summary>
    protected internal void InvokePointerLeft() => PointerLeft?.Invoke();

    /// <summary>
    /// Called to measure the chart.
    /// </summary>
    /// <returns>The update task.</returns>
    protected virtual Task UpdateThrottlerUnlocked()
    {
        return Task.Run(() =>
        {
            View.InvokeOnUIThread(() =>
            {
                lock (View.CoreCanvas.Sync)
                {
                    if (_isUnloaded) return;
                    Measure();
                }
            });
        });
    }

    /// <summary>
    /// Measures the chart.
    /// </summary>
    protected internal void Measure()
    {
        if (_activeMap is not null && _activeMap != View.ActiveMap)
        {
            _previousStroke?.ClearGeometriesFromPaintTask(View.CoreCanvas);
            _previousFill?.ClearGeometriesFromPaintTask(View.CoreCanvas);

            _previousFill = null;
            _previousStroke = null;

            View.CoreCanvas.Clear();
        }
        _activeMap = View.ActiveMap;

        if (!_isHeatInCanvas)
        {
            View.CoreCanvas.AddDrawableTask(_heatPaint);
            _isHeatInCanvas = true;
        }

        if (_previousStroke != View.Stroke)
        {
            if (_previousStroke is not null)
                View.CoreCanvas.RemovePaintTask(_previousStroke);

            if (View.Stroke is not null)
            {
                if (View.Stroke.ZIndex == 0) View.Stroke.ZIndex = PaintConstants.GeoMapStrokeZIndex;
                View.Stroke.PaintStyle = PaintStyle.Stroke;
                View.CoreCanvas.AddDrawableTask(View.Stroke);
            }

            _previousStroke = View.Stroke;
        }

        if (_previousFill != View.Fill)
        {
            if (_previousFill is not null)
                View.CoreCanvas.RemovePaintTask(_previousFill);

            if (View.Fill is not null)
            {
                View.Fill.PaintStyle = PaintStyle.Fill;
                View.CoreCanvas.AddDrawableTask(View.Fill);
            }

            _previousFill = View.Fill;
        }

        var i = _previousFill?.ZIndex ?? 0;
        _heatPaint.ZIndex = i + 1;

        var context = new MapContext(
            this, View, View.ActiveMap,
            Maps.BuildProjector(View.MapProjection, [View.ControlSize.Width, View.ControlSize.Height]));

        _mapFactory.GenerateLands(context);

        var toDeleteSeries = new HashSet<IGeoSeries>(_everMeasuredSeries);
        foreach (var series in View.Series?.Cast<IGeoSeries>() ?? [])
        {
            series.Measure(context);
            _ = _everMeasuredSeries.Add(series);
            _ = toDeleteSeries.Remove(series);
        }

        foreach (var series in toDeleteSeries)
        {
            series.Delete(context);
            _ = _everMeasuredSeries.Remove(series);
        }

        // Refresh tooltip if a land is currently hovered (data may have changed)
        if (_hoveredLand is not null && View.Tooltip is not null &&
            View.TooltipPosition != TooltipPosition.Hidden)
        {
            var value = 0d;
            foreach (var series in View.Series?.Cast<IGeoSeries>() ?? [])
            {
                if (series.TryGetValue(_hoveredLand.ShortName, out value))
                    break;
            }

            // Compute screen-space center from the first data shape
            var center = _pointerPosition;
            foreach (var data in _hoveredLand.Data)
            {
                if (data.Shape is null) continue;
                float minX = float.MaxValue, minY = float.MaxValue;
                float maxX = float.MinValue, maxY = float.MinValue;
                foreach (var seg in data.Shape.Commands)
                {
                    if (seg.Xi < minX) minX = seg.Xi;
                    if (seg.Yi < minY) minY = seg.Yi;
                    if (seg.Xi > maxX) maxX = seg.Xi;
                    if (seg.Yi > maxY) maxY = seg.Yi;
                }
                center = new LvcPoint((minX + maxX) / 2f, (minY + maxY) / 2f);
                break;
            }

            View.Tooltip.Show(
                new GeoTooltipPoint
                {
                    Land = _hoveredLand,
                    Value = value,
                    LandCenter = center
                },
                this);
        }

        View.CoreCanvas.Invalidate();
    }

    private Task PanningThrottlerUnlocked()
    {
        return Task.Run(() =>
            View.InvokeOnUIThread(() =>
            {
                lock (View.CoreCanvas.Sync)
                {
                    Pan(
                        new LvcPoint(
                            (float)(_pointerPanningPosition.X - _pointerPreviousPanningPosition.X),
                            (float)(_pointerPanningPosition.Y - _pointerPreviousPanningPosition.Y)));
                    _pointerPreviousPanningPosition = new LvcPoint(_pointerPanningPosition.X, _pointerPanningPosition.Y);
                }
            }));
    }

    /// <summary>
    /// Finds the land definition at the specified pointer position, if any.
    /// </summary>
    /// <param name="pointerPosition">The pointer position in control coordinates.</param>
    /// <returns>A tuple of the land definition, heat value, and screen-space center, or null.</returns>
    public (LandDefinition Land, double Value, LvcPoint Center)? FindLandAt(LvcPoint pointerPosition)
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

                    // Look up the heat value from the series
                    var value = 0d;
                    foreach (var series in View.Series?.Cast<IGeoSeries>() ?? [])
                    {
                        if (series.TryGetValue(landDefinition.ShortName, out value))
                            break;
                    }

                    // Compute screen-space center from the shape's segments
                    var commands = landData.Shape.Commands;
                    float minX = float.MaxValue, minY = float.MaxValue;
                    float maxX = float.MinValue, maxY = float.MinValue;
                    foreach (var seg in commands)
                    {
                        if (seg.Xi < minX) minX = seg.Xi;
                        if (seg.Yi < minY) minY = seg.Yi;
                        if (seg.Xi > maxX) maxX = seg.Xi;
                        if (seg.Yi > maxY) maxY = seg.Yi;
                    }
                    var center = new LvcPoint((minX + maxX) / 2f, (minY + maxY) / 2f);

                    return (landDefinition, value, center);
                }
            }
        }

        return null;
    }

    private Task TooltipThrottlerUnlocked()
    {
        return Task.Run(() =>
            View.InvokeOnUIThread(() =>
            {
                lock (View.CoreCanvas.Sync)
                {
                    if (_isUnloaded || _isPanning || !_isPointerIn) return;
                    if (View.Tooltip is null || View.TooltipPosition == TooltipPosition.Hidden) return;

                    var result = FindLandAt(_pointerPosition);

                    if (result is null)
                    {
                        if (_isToolTipOpen)
                        {
                            _hoveredLand = null;
                            _isToolTipOpen = false;
                            View.Tooltip.Hide(this);
                            View.CoreCanvas.Invalidate();
                        }
                        return;
                    }

                    var (land, value, center) = result.Value;

                    if (land == _hoveredLand) return;
                    _hoveredLand = land;
                    _isToolTipOpen = true;

                    View.Tooltip.Show(
                        new GeoTooltipPoint
                        {
                            Land = land,
                            Value = value,
                            LandCenter = center
                        },
                        this);
                    View.CoreCanvas.Invalidate();
                }
            }));
    }

    private void Chart_PointerDown(LvcPoint pointerPosition)
    {
        _isPanning = true;
        _pointerPreviousPanningPosition = pointerPosition;

        // Hide tooltip while panning
        if (_isToolTipOpen)
        {
            _hoveredLand = null;
            _isToolTipOpen = false;
            View.Tooltip?.Hide(this);
        }
    }

    private void Chart_PointerMove(LvcPoint pointerPosition)
    {
        _pointerPosition = pointerPosition;
        _isPointerIn = true;

        if (_isPanning)
        {
            _pointerPanningPosition = pointerPosition;
            _panningThrottler.Call();
        }
        else
        {
            _tooltipThrottler.Call();
        }
    }

    private void Chart_PointerLeft()
    {
        _isPointerIn = false;

        if (_isToolTipOpen)
        {
            _hoveredLand = null;
            _isToolTipOpen = false;
            View.InvokeOnUIThread(() =>
            {
                View.Tooltip?.Hide(this);
                View.CoreCanvas.Invalidate();
            });
        }
    }

    private void Chart_PointerUp(LvcPoint pointerPosition)
    {
        if (!_isPanning) return;
        _isPanning = false;
        _panningThrottler.Call();
    }
}
