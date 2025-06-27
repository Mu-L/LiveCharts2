﻿// The MIT License(MIT)
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.VisualElements;

namespace LiveChartsCore.SkiaSharpView.WinForms;

/// <inheritdoc cref="IPolarChartView" />
public class PolarChart : Chart, IPolarChartView
{
    private bool _fitToBounds = false;
    private double _totalAngle = 360;
    private double _innerRadius;
    private double _initialRotation = LiveCharts.DefaultSettings.PolarInitialRotation;
    private ICollection<ISeries> _series = [];
    private ICollection<IPolarAxis> _angleAxes = [];
    private ICollection<IPolarAxis> _radiusAxes = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="PolarChart"/> class.
    /// </summary>
    public PolarChart() : this(null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PolarChart"/> class.
    /// </summary>
    /// <param name="tooltip">The default tool tip control.</param>
    /// <param name="legend">The default legend control.</param>
    public PolarChart(IChartTooltip? tooltip = null, IChartLegend? legend = null)
        : base(tooltip, legend)
    {
        _ = Observe
            .Collection(nameof(Series))
            .Collection(nameof(AngleAxes))
            .Collection(nameof(RadiusAxes));

        AngleAxes = new ObservableCollection<IPolarAxis>();
        RadiusAxes = new ObservableCollection<IPolarAxis>();
        Series = new ObservableCollection<ISeries>();
        VisualElements = new ObservableCollection<IChartElement>();

        var c = Controls[0].Controls[0];

        c.MouseWheel += OnMouseWheel;
        c.MouseDown += OnMouseDown;
        c.MouseUp += OnMouseUp;
    }

    PolarChartEngine IPolarChartView.Core =>
        core is null ? throw new Exception("core not found") : (PolarChartEngine)core;

    /// <inheritdoc cref="IPolarChartView.FitToBounds" />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool FitToBounds
    {
        get => _fitToBounds;
        set
        {
            _fitToBounds = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc cref="IPolarChartView.TotalAngle" />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double TotalAngle
    {
        get => _totalAngle;
        set
        {
            _totalAngle = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc cref="IPolarChartView.InnerRadius" />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double InnerRadius
    {
        get => _innerRadius;
        set
        {
            _innerRadius = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc cref="IPolarChartView.InitialRotation" />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double InitialRotation
    {
        get => _initialRotation;
        set
        {
            _initialRotation = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc cref="IPolarChartView.Series" />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ICollection<ISeries> Series
    {
        get => _series;
        set
        {
            _series = value;
            Observe[nameof(Series)].Initialize(value);
        }
    }

    /// <inheritdoc cref="IPolarChartView.AngleAxes" />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ICollection<IPolarAxis> AngleAxes
    {
        get => _angleAxes;
        set
        {
            _angleAxes = value;
            Observe[nameof(AngleAxes)].Initialize(value);
        }
    }

    /// <inheritdoc cref="IPolarChartView.RadiusAxes" />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ICollection<IPolarAxis> RadiusAxes
    {
        get => _radiusAxes;
        set
        {
            _radiusAxes = value;
            Observe[nameof(RadiusAxes)].Initialize(value);
        }
    }

    /// <summary>
    /// Initializes the core.
    /// </summary>
    protected override void InitializeCore()
    {
        core = new PolarChartEngine(
            this, config => config.UseDefaults(), motionCanvas.CanvasCore);
        if (((IChartView)this).DesignerMode) return;
        core.Update();
    }

    /// <inheritdoc cref="IPolarChartView.ScalePixelsToData(LvcPointD, int, int)"/>
    public LvcPointD ScalePixelsToData(LvcPointD point, int angleAxisIndex = 0, int radiusAxisIndex = 0)
    {
        if (core is not PolarChartEngine cc) throw new Exception("core not found");

        var scaler = new PolarScaler(
            cc.DrawMarginLocation, cc.DrawMarginSize, cc.AngleAxes[angleAxisIndex], cc.RadiusAxes[radiusAxisIndex],
            cc.InnerRadius, cc.InitialRotation, cc.TotalAnge);

        return scaler.ToChartValues(point.X, point.Y);
    }

    /// <inheritdoc cref="IPolarChartView.ScaleDataToPixels(LvcPointD, int, int)"/>
    public LvcPointD ScaleDataToPixels(LvcPointD point, int angleAxisIndex = 0, int radiusAxisIndex = 0)
    {
        if (core is not PolarChartEngine cc) throw new Exception("core not found");

        var scaler = new PolarScaler(
            cc.DrawMarginLocation, cc.DrawMarginSize, cc.AngleAxes[angleAxisIndex], cc.RadiusAxes[radiusAxisIndex],
            cc.InnerRadius, cc.InitialRotation, cc.TotalAnge);

        var r = scaler.ToPixels(point.X, point.Y);

        return new LvcPointD { X = (float)r.X, Y = (float)r.Y };
    }

    /// <inheritdoc cref="IChartView.GetPointsAt(LvcPointD, FindingStrategy, FindPointFor)"/>
    public override IEnumerable<ChartPoint> GetPointsAt(LvcPointD point, FindingStrategy strategy = FindingStrategy.Automatic, FindPointFor findPointFor = FindPointFor.HoverEvent)
    {
        if (core is not PolarChartEngine cc) throw new Exception("core not found");

        if (strategy == FindingStrategy.Automatic)
            strategy = cc.Series.GetFindingStrategy();

        return cc.Series.SelectMany(series => series.FindHitPoints(cc, new(point), strategy, findPointFor));
    }

    /// <inheritdoc cref="IChartView.GetVisualsAt(LvcPointD)"/>
    public override IEnumerable<IChartElement> GetVisualsAt(LvcPointD point)
    {
        return core is not PolarChartEngine cc
            ? throw new Exception("core not found")
            : cc.VisualElements.SelectMany(visual => ((VisualElement)visual).IsHitBy(core, new(point)));
    }

    private void OnMouseWheel(object? sender, MouseEventArgs e)
    {
        //if (core is null) throw new Exception("core not found");
        //var c = (PolarChart<SkiaSharpDrawingContext>)core;
        //var p = e.Location;
        //c.Zoom(new PointF(p.X, p.Y), e.Delta > 0 ? ZoomDirection.ZoomIn : ZoomDirection.ZoomOut);
        //Capture = true;
    }

    private void OnMouseDown(object? sender, MouseEventArgs e) =>
        core?.InvokePointerDown(new(e.Location.X, e.Location.Y), false);

    private void OnMouseUp(object? sender, MouseEventArgs e) =>
        core?.InvokePointerUp(new(e.Location.X, e.Location.Y), false);
}
