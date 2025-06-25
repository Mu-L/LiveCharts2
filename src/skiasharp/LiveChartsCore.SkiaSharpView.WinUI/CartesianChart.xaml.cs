﻿
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using LiveChartsCore.Drawing;
using LiveChartsCore.Generators;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Observers;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.Motion;
using LiveChartsCore.Painting;
using LiveChartsCore.SkiaSharpView.TypeConverters;
using LiveChartsCore.Themes;
using LiveChartsCore.VisualElements;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LiveChartsCore.SkiaSharpView.WinUI;

/// <inheritdoc cref="IChartView" />
public sealed partial class CartesianChart : UserControl, ICartesianChartView
{
    #region fields

    private Chart? _core;
    private readonly MotionCanvas _canvas;
    private readonly ChartObserver _observe;
    private bool _matchAxesScreenDataRatio;
    private ThemeListener? _themeListener;
    private Theme? _chartTheme;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="CartesianChart"/> class.
    /// </summary>
    public CartesianChart()
    {
        LiveCharts.Configure(config => config.UseDefaults());

        InitializeComponent();

        var canvas = (MotionCanvas)FindName("motionCanvas");
        _canvas = canvas;

        _observe = new ChartObserver(() => _core?.Update(), AddUIElement, RemoveUIElement)
            .Collection(nameof(Series))
            .Collection(nameof(XAxes))
            .Collection(nameof(YAxes))
            .Collection(nameof(Sections))
            .Collection(nameof(VisualElements))
            .Property(nameof(Title))
            .Property(nameof(DrawMarginFrame));

        _observe.Add(
            nameof(SeriesSource),
            new SeriesSourceObserver(
                InflateSeriesTemplate,
                GetSeriesSource,
                () => SeriesSource is not null && SeriesTemplate is not null));

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        SetValue(XAxesProperty, new ObservableCollection<ICartesianAxis>());
        SetValue(YAxesProperty, new ObservableCollection<ICartesianAxis>());
        SetValue(SeriesProperty, new ObservableCollection<ISeries>());
        SetValue(SectionsProperty, new ObservableCollection<IChartElement>());
        SetValue(VisualElementsProperty, new ObservableCollection<IChartElement>());
        SetValue(SyncContextProperty, new());
    }


    #region Generated Bindable Properties

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0052 // Remove unread private member

    private static readonly XamlProperty<IEnumerable<object>> seriesSource = new(onChanged: OnSeriesSourceChanged);
    private static readonly XamlProperty<DataTemplate> seriesTemplate = new(onChanged: OnSeriesSourceChanged);

#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore IDE0052 // Remove unread private members

    #endregion

    #region dependency properties

    /// <summary>
    /// The series property.
    /// </summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title), typeof(IChartElement), typeof(CartesianChart), new PropertyMetadata(null));

    /// <summary>
    /// The series property
    /// </summary>
    public static readonly DependencyProperty SeriesProperty =
        DependencyProperty.Register(
            nameof(Series), typeof(ICollection<ISeries>), typeof(CartesianChart),
            new PropertyMetadata(null, InitializeObserver(nameof(Series))));

    /// <summary>
    /// The x axes property
    /// </summary>
    public static readonly DependencyProperty XAxesProperty =
        DependencyProperty.Register(
            nameof(XAxes), typeof(ICollection<ICartesianAxis>), typeof(CartesianChart),
            new PropertyMetadata(null, InitializeObserver(nameof(XAxes))));

    /// <summary>
    /// The y axes property
    /// </summary>
    public static readonly DependencyProperty YAxesProperty =
        DependencyProperty.Register(
            nameof(YAxes), typeof(ICollection<ICartesianAxis>), typeof(CartesianChart),
            new PropertyMetadata(null, InitializeObserver(nameof(YAxes))));

    /// <summary>
    /// The sections property
    /// </summary>
    public static readonly DependencyProperty SectionsProperty =
        DependencyProperty.Register(
            nameof(Sections), typeof(ICollection<IChartElement>), typeof(CartesianChart),
            new PropertyMetadata(null, InitializeObserver(nameof(Sections))));

    /// <summary>
    /// The visual elements property
    /// </summary>
    public static readonly DependencyProperty VisualElementsProperty =
        DependencyProperty.Register(
            nameof(VisualElements), typeof(ICollection<IChartElement>), typeof(CartesianChart),
            new PropertyMetadata(null, InitializeObserver(nameof(VisualElements))));

    /// <summary>
    /// The sync context property
    /// </summary>
    public static readonly DependencyProperty SyncContextProperty =
        DependencyProperty.Register(
            nameof(SyncContext), typeof(object), typeof(CartesianChart), new PropertyMetadata(null,
                (o, args) =>
                {
                    var chart = (CartesianChart)o;
                    if (chart._canvas != null) chart.CoreCanvas.Sync = args.NewValue;
                    if (chart._core == null) return;
                    chart._core.Update();
                }));

    /// <summary>
    /// The zoom mode property
    /// </summary>
    public static readonly DependencyProperty DrawMarginFrameProperty =
        DependencyProperty.Register(
            nameof(DrawMarginFrame), typeof(IChartElement), typeof(CartesianChart),
            new PropertyMetadata(null, InitializeObserver(nameof(DrawMarginFrame))));

    /// <summary>
    /// The zoom mode property
    /// </summary>
    public static readonly DependencyProperty ZoomModeProperty =
        DependencyProperty.Register(
            nameof(ZoomMode), typeof(ZoomAndPanMode), typeof(CartesianChart),
            new PropertyMetadata(LiveCharts.DefaultSettings.ZoomMode));

    /// <summary>
    /// The zooming speed property
    /// </summary>
    public static readonly DependencyProperty ZoomingSpeedProperty =
        DependencyProperty.Register(
            nameof(ZoomingSpeed), typeof(double), typeof(CartesianChart),
            new PropertyMetadata(LiveCharts.DefaultSettings.ZoomSpeed));

    /// <summary>
    /// The tool tip finding strategy property
    /// </summary>
    public static readonly DependencyProperty FindingStrategyProperty =
        DependencyProperty.Register(
            nameof(FindingStrategy), typeof(FindingStrategy), typeof(CartesianChart),
            new PropertyMetadata(LiveCharts.DefaultSettings.FindingStrategy, OnDependencyPropertyChanged));

    /// <summary>
    /// The draw margin property
    /// </summary>
    public static readonly DependencyProperty DrawMarginProperty =
       DependencyProperty.Register(
           nameof(DrawMargin), typeof(Margin), typeof(CartesianChart), new PropertyMetadata(null, OnDependencyPropertyChanged));

    /// <summary>
    /// The animations speed property
    /// </summary>
    public static readonly DependencyProperty AnimationsSpeedProperty =
        DependencyProperty.Register(
            nameof(AnimationsSpeed), typeof(TimeSpan), typeof(CartesianChart),
            new PropertyMetadata(LiveCharts.DefaultSettings.AnimationsSpeed, OnDependencyPropertyChanged));

    /// <summary>
    /// The easing function property
    /// </summary>
    public static readonly DependencyProperty EasingFunctionProperty =
        DependencyProperty.Register(
            nameof(EasingFunction), typeof(Func<float, float>), typeof(CartesianChart),
            new PropertyMetadata(LiveCharts.DefaultSettings.EasingFunction, OnDependencyPropertyChanged));

    /// <summary>
    /// The legend position property
    /// </summary>
    public static readonly DependencyProperty LegendPositionProperty =
        DependencyProperty.Register(
            nameof(LegendPosition), typeof(LegendPosition), typeof(CartesianChart),
            new PropertyMetadata(LiveCharts.DefaultSettings.LegendPosition, OnDependencyPropertyChanged));

    /// <summary>
    /// The legend background paint property
    /// </summary>
    public static readonly DependencyProperty LegendBackgroundPaintProperty =
        DependencyProperty.Register(
            nameof(LegendBackgroundPaint), typeof(Paint), typeof(CartesianChart),
            new PropertyMetadata(LiveCharts.DefaultSettings.LegendBackgroundPaint, OnDependencyPropertyChanged));

    /// <summary>
    /// The legend text paint property
    /// </summary>
    public static readonly DependencyProperty LegendTextPaintProperty =
        DependencyProperty.Register(
            nameof(LegendTextPaint), typeof(Paint), typeof(CartesianChart),
            new PropertyMetadata(LiveCharts.DefaultSettings.LegendTextPaint, OnDependencyPropertyChanged));

    /// <summary>
    /// The legend text size property
    /// </summary>
    public static readonly DependencyProperty LegendTextSizeProperty =
        DependencyProperty.Register(
            nameof(LegendTextSize), typeof(double), typeof(CartesianChart),
            new PropertyMetadata(LiveCharts.DefaultSettings.LegendTextSize, OnDependencyPropertyChanged));

    /// <summary>
    /// The tool tip position property
    /// </summary>
    public static readonly DependencyProperty TooltipPositionProperty =
       DependencyProperty.Register(
           nameof(TooltipPosition), typeof(TooltipPosition), typeof(CartesianChart),
           new PropertyMetadata(LiveCharts.DefaultSettings.TooltipPosition, OnDependencyPropertyChanged));

    /// <summary>
    /// The tooltip background paint property
    /// </summary>
    public static readonly DependencyProperty TooltipBackgroundPaintProperty =
        DependencyProperty.Register(
            nameof(TooltipBackgroundPaint), typeof(Paint), typeof(CartesianChart),
            new PropertyMetadata(LiveCharts.DefaultSettings.TooltipBackgroundPaint, OnDependencyPropertyChanged));

    /// <summary>
    /// The tooltip text paint property
    /// </summary>
    public static readonly DependencyProperty TooltipTextPaintProperty =
        DependencyProperty.Register(
            nameof(TooltipTextPaint), typeof(Paint), typeof(CartesianChart),
            new PropertyMetadata(LiveCharts.DefaultSettings.TooltipTextPaint, OnDependencyPropertyChanged));

    /// <summary>
    /// The tooltip text size property
    /// </summary>
    public static readonly DependencyProperty TooltipTextSizeProperty =
        DependencyProperty.Register(
            nameof(TooltipTextSize), typeof(double), typeof(CartesianChart),
            new PropertyMetadata(LiveCharts.DefaultSettings.TooltipTextSize, OnDependencyPropertyChanged));

    /// <summary>
    /// The update started command.
    /// </summary>
    public static readonly DependencyProperty UpdateStartedCommandProperty =
       DependencyProperty.Register(
           nameof(UpdateStartedCommand), typeof(ICommand), typeof(CartesianChart),
           new PropertyMetadata(null));

    /// <summary>
    /// The pointer pressed command.
    /// </summary>
    public static readonly DependencyProperty PointerPressedCommandProperty =
       DependencyProperty.Register(
           nameof(PointerPressedCommand), typeof(ICommand), typeof(CartesianChart),
           new PropertyMetadata(null));

    /// <summary>
    /// The pointer released command.
    /// </summary>
    public static readonly DependencyProperty PointerReleasedCommandProperty =
       DependencyProperty.Register(
           nameof(PointerReleasedCommand), typeof(ICommand), typeof(CartesianChart),
           new PropertyMetadata(null));

    /// <summary>
    /// The pointer move command.
    /// </summary>
    public static readonly DependencyProperty PointerMoveCommandProperty =
       DependencyProperty.Register(
           nameof(PointerMoveCommand), typeof(ICommand), typeof(CartesianChart),
           new PropertyMetadata(null));

    /// <summary>
    /// The data pointer down command property
    /// </summary>
    public static readonly DependencyProperty DataPointerDownCommandProperty =
        DependencyProperty.Register(
            nameof(DataPointerDownCommand), typeof(ICommand), typeof(CartesianChart), new PropertyMetadata(null));

    /// <summary>
    /// The hovered points chaanged command property
    /// </summary>
    public static readonly DependencyProperty HoveredPointsChangedCommandProperty =
        DependencyProperty.Register(
            nameof(HoveredPointsChangedCommand), typeof(ICommand), typeof(CartesianChart), new PropertyMetadata(null));

    /// <summary>
    /// The chart point pointer down command property
    /// </summary>
    [Obsolete($"Use the {nameof(DataPointerDown)} event instead with a {nameof(FindingStrategy)} that used TakeClosest.")]
    public static readonly DependencyProperty ChartPointPointerDownCommandProperty =
        DependencyProperty.Register(
            nameof(ChartPointPointerDownCommand), typeof(ICommand), typeof(CartesianChart), new PropertyMetadata(null));

    /// <summary>
    /// The visual elements pointer down command property
    /// </summary>
    public static readonly DependencyProperty VisualElementsPointerDownCommandProperty =
        DependencyProperty.Register(
            nameof(VisualElementsPointerDownCommand), typeof(ICommand), typeof(CartesianChart), new PropertyMetadata(null));

    #endregion

    #region events

    /// <inheritdoc cref="IChartView.Measuring" />
    public event ChartEventHandler? Measuring;

    /// <inheritdoc cref="IChartView.UpdateStarted" />
    public event ChartEventHandler? UpdateStarted;

    /// <inheritdoc cref="IChartView.UpdateFinished" />
    public event ChartEventHandler? UpdateFinished;

    /// <inheritdoc cref="IChartView.DataPointerDown" />
    public event ChartPointsHandler? DataPointerDown;

    /// <inheritdoc cref="IChartView.HoveredPointsChanged" />
    public event ChartPointHoverHandler? HoveredPointsChanged;

    /// <inheritdoc cref="IChartView.ChartPointPointerDown" />
    [Obsolete($"Use the {nameof(DataPointerDown)} event instead with a {nameof(FindingStrategy)} that used TakeClosest.")]
    public event ChartPointHandler? ChartPointPointerDown;

    /// <inheritdoc cref="IChartView.VisualElementsPointerDown"/>
    public event VisualElementsHandler? VisualElementsPointerDown;

    #endregion

    #region properties

    bool IChartView.DesignerMode => Windows.ApplicationModel.DesignMode.DesignModeEnabled;

    bool IChartView.IsDarkMode => Application.Current?.RequestedTheme == ApplicationTheme.Dark;

    /// <inheritdoc cref="IChartView.ChartTheme" />
    public Theme? ChartTheme { get => _chartTheme; set { _chartTheme = value; _core?.Update(); } }

    /// <inheritdoc cref="IChartView.CoreChart" />
    public Chart CoreChart => _core ?? throw new Exception("Core not set yet.");

    LvcColor IChartView.BackColor
    {
        get => Background is not SolidColorBrush b
            ? new LvcColor()
            : LvcColor.FromArgb(b.Color.A, b.Color.R, b.Color.G, b.Color.B);
        set => SetValue(BackgroundProperty, new SolidColorBrush(Windows.UI.Color.FromArgb(value.A, value.R, value.G, value.B)));
    }

    /// <inheritdoc cref="IChartView.DrawMargin" />
    [TypeConverter(typeof(MarginTypeConverter))]
    public Margin? DrawMargin
    {
        get => (Margin)GetValue(DrawMarginProperty);
        set => SetValue(DrawMarginProperty, value);
    }

    Margin? IChartView.DrawMargin
    {
        get => DrawMargin;
        set => SetValue(DrawMarginProperty, value);
    }

    LvcSize IChartView.ControlSize => _canvas == null
                ? throw new Exception("Canvas not found")
                : new LvcSize { Width = (float)_canvas.ActualWidth, Height = (float)_canvas.ActualHeight };

    /// <inheritdoc cref="IChartView.CoreCanvas" />
    public CoreMotionCanvas CoreCanvas => _canvas == null ? throw new Exception("Canvas not found") : _canvas.CanvasCore;

    CartesianChartEngine ICartesianChartView.Core =>
        _core == null ? throw new Exception("core not found") : (CartesianChartEngine)_core;

    /// <inheritdoc cref="IChartView.SyncContext" />
    public object SyncContext
    {
        get => GetValue(SyncContextProperty);
        set => SetValue(SyncContextProperty, value);
    }

    /// <inheritdoc cref="IChartView.Title" />
    public IChartElement? Title
    {
        get => (IChartElement?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <inheritdoc cref="ICartesianChartView.Series" />
    public ICollection<ISeries> Series
    {
        get => (ICollection<ISeries>)GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    /// <inheritdoc cref="ICartesianChartView.XAxes" />
    public ICollection<ICartesianAxis> XAxes
    {
        get => (ICollection<ICartesianAxis>)GetValue(XAxesProperty);
        set => SetValue(XAxesProperty, value);
    }

    /// <inheritdoc cref="ICartesianChartView.YAxes" />
    public ICollection<ICartesianAxis> YAxes
    {
        get => (ICollection<ICartesianAxis>)GetValue(YAxesProperty);
        set => SetValue(YAxesProperty, value);
    }

    /// <inheritdoc cref="ICartesianChartView.Sections" />
    public ICollection<IChartElement> Sections
    {
        get => (ICollection<IChartElement>)GetValue(SectionsProperty);
        set => SetValue(SectionsProperty, value);
    }

    /// <inheritdoc cref="IChartView.VisualElements" />
    public ICollection<IChartElement> VisualElements
    {
        get => (ICollection<IChartElement>)GetValue(VisualElementsProperty);
        set => SetValue(VisualElementsProperty, value);
    }

    /// <inheritdoc cref="ICartesianChartView.DrawMarginFrame" />
    public IChartElement? DrawMarginFrame
    {
        get => (IChartElement)GetValue(DrawMarginFrameProperty);
        set => SetValue(DrawMarginFrameProperty, value);
    }

    /// <inheritdoc cref="ICartesianChartView.ZoomMode" />
    public ZoomAndPanMode ZoomMode
    {
        get => (ZoomAndPanMode)GetValue(ZoomModeProperty);
        set => SetValue(ZoomModeProperty, value);
    }

    ZoomAndPanMode ICartesianChartView.ZoomMode
    {
        get => ZoomMode;
        set => SetValue(ZoomModeProperty, value);
    }

    /// <inheritdoc cref="ICartesianChartView.ZoomingSpeed" />
    public double ZoomingSpeed
    {
        get => (double)GetValue(ZoomingSpeedProperty);
        set => SetValue(ZoomingSpeedProperty, value);
    }

    double ICartesianChartView.ZoomingSpeed
    {
        get => ZoomingSpeed;
        set => SetValue(ZoomingSpeedProperty, value);
    }

    /// <inheritdoc cref="ICartesianChartView.FindingStrategy" />
    [Obsolete($"Renamed to {nameof(FindingStrategy)}")]
    public TooltipFindingStrategy TooltipFindingStrategy
    {
        get => ((FindingStrategy)GetValue(FindingStrategyProperty)!).AsOld();
        set => SetValue(FindingStrategyProperty, value.AsNew());
    }

    /// <inheritdoc cref="ICartesianChartView.FindingStrategy" />
    public FindingStrategy FindingStrategy
    {
        get => (FindingStrategy)GetValue(FindingStrategyProperty);
        set => SetValue(FindingStrategyProperty, value);
    }

    /// <inheritdoc cref="IChartView.AnimationsSpeed" />
    public TimeSpan AnimationsSpeed
    {
        get => (TimeSpan)GetValue(AnimationsSpeedProperty);
        set => SetValue(AnimationsSpeedProperty, value);
    }

    TimeSpan IChartView.AnimationsSpeed
    {
        get => AnimationsSpeed;
        set => SetValue(AnimationsSpeedProperty, value);
    }

    /// <inheritdoc cref="IChartView.EasingFunction" />
    public Func<float, float> EasingFunction
    {
        get => (Func<float, float>)GetValue(EasingFunctionProperty);
        set => SetValue(EasingFunctionProperty, value);
    }

    Func<float, float>? IChartView.EasingFunction
    {
        get => EasingFunction;
        set => SetValue(EasingFunctionProperty, value);
    }

    /// <inheritdoc cref="IChartView.LegendPosition" />
    public LegendPosition LegendPosition
    {
        get => (LegendPosition)GetValue(LegendPositionProperty);
        set => SetValue(LegendPositionProperty, value);
    }

    /// <inheritdoc cref="IChartView.LegendBackgroundPaint" />
    [TypeConverter(typeof(HexToPaintTypeConverter))]
    public Paint? LegendBackgroundPaint
    {
        get => (Paint?)GetValue(LegendBackgroundPaintProperty);
        set => SetValue(LegendBackgroundPaintProperty, value);
    }

    /// <inheritdoc cref="IChartView.LegendTextPaint" />
    [TypeConverter(typeof(HexToPaintTypeConverter))]
    public Paint? LegendTextPaint
    {
        get => (Paint?)GetValue(LegendTextPaintProperty);
        set => SetValue(LegendTextPaintProperty, value);
    }

    /// <inheritdoc cref="IChartView.LegendTextSize" />
    public double LegendTextSize
    {
        get => (double?)GetValue(LegendTextSizeProperty) ?? LiveCharts.DefaultSettings.LegendTextSize;
        set => SetValue(LegendTextSizeProperty, value);
    }

    /// <inheritdoc cref="IChartView.Legend" />
    public IChartLegend? Legend { get; set; }

    /// <inheritdoc cref="IChartView.TooltipPosition" />
    public TooltipPosition TooltipPosition
    {
        get => (TooltipPosition)GetValue(TooltipPositionProperty);
        set => SetValue(TooltipPositionProperty, value);
    }

    /// <inheritdoc cref="IChartView.TooltipBackgroundPaint" />
    [TypeConverter(typeof(HexToPaintTypeConverter))]
    public Paint? TooltipBackgroundPaint
    {
        get => (Paint?)GetValue(TooltipBackgroundPaintProperty);
        set => SetValue(TooltipBackgroundPaintProperty, value);
    }

    /// <inheritdoc cref="IChartView.TooltipTextPaint" />
    [TypeConverter(typeof(HexToPaintTypeConverter))]
    public Paint? TooltipTextPaint
    {
        get => (Paint?)GetValue(TooltipTextPaintProperty);
        set => SetValue(TooltipTextPaintProperty, value);
    }

    /// <inheritdoc cref="IChartView.TooltipTextSize" />
    public double TooltipTextSize
    {
        get => (double?)GetValue(TooltipTextSizeProperty) ?? LiveCharts.DefaultSettings.TooltipTextSize;
        set => SetValue(TooltipTextSizeProperty, value);
    }

    /// <inheritdoc cref="IChartView.Tooltip" />
    public IChartTooltip? Tooltip { get; set; }

    /// <inheritdoc cref="IChartView.AutoUpdateEnabled" />
    public bool AutoUpdateEnabled { get; set; } = true;

    /// <inheritdoc cref="IChartView.UpdaterThrottler" />
    public TimeSpan UpdaterThrottler { get; set; } = LiveCharts.DefaultSettings.UpdateThrottlingTimeout;

    /// <summary>
    /// Gets or sets a command to execute when the chart update started.
    /// </summary>
    public ICommand? UpdateStartedCommand
    {
        get => (ICommand?)GetValue(UpdateStartedCommandProperty);
        set => SetValue(UpdateStartedCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets a command to execute when the pointer is pressed on the chart.
    /// </summary>
    public ICommand? PointerPressedCommand
    {
        get => (ICommand?)GetValue(PointerPressedCommandProperty);
        set => SetValue(PointerPressedCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets a command to execute when the pointer is released on the chart.
    /// </summary>
    public ICommand? PointerReleasedCommand
    {
        get => (ICommand?)GetValue(PointerReleasedCommandProperty);
        set => SetValue(PointerReleasedCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets a command to execute when the pointer moves over the chart.
    /// </summary>
    public ICommand? PointerMoveCommand
    {
        get => (ICommand?)GetValue(PointerMoveCommandProperty);
        set => SetValue(PointerMoveCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets a command to execute when the pointer goes down on a data or data points.
    /// </summary>
    public ICommand? DataPointerDownCommand
    {
        get => (ICommand?)GetValue(DataPointerDownCommandProperty);
        set => SetValue(DataPointerDownCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets a command to execute when the hovered points change.
    /// </summary>
    public ICommand? HoveredPointsChangedCommand
    {
        get => (ICommand?)GetValue(HoveredPointsChangedCommandProperty);
        set => SetValue(HoveredPointsChangedCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets a command to execute when the pointer goes down on a chart point.
    /// </summary>
    [Obsolete($"Use the {nameof(DataPointerDown)} event instead with a {nameof(FindingStrategy)} that used TakeClosest.")]
    public ICommand? ChartPointPointerDownCommand
    {
        get => (ICommand?)GetValue(ChartPointPointerDownCommandProperty);
        set => SetValue(ChartPointPointerDownCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets a command to execute when the pointer goes down on a chart point.
    /// </summary>
    public ICommand? VisualElementsPointerDownCommand
    {
        get => (ICommand?)GetValue(VisualElementsPointerDownCommandProperty);
        set => SetValue(VisualElementsPointerDownCommandProperty, value);
    }

    /// <inheritdoc cref="ICartesianChartView.MatchAxesScreenDataRatio" />
    public bool MatchAxesScreenDataRatio
    {
        get => _matchAxesScreenDataRatio;
        set
        {
            _matchAxesScreenDataRatio = value;
            OnMatchAxesScreenDataRatioChanged();
        }
    }

    #endregion

    /// <inheritdoc cref="ICartesianChartView.ScalePixelsToData(LvcPointD, int, int)"/>
    public LvcPointD ScalePixelsToData(LvcPointD point, int xAxisIndex = 0, int yAxisIndex = 0)
    {
        if (_core is not CartesianChartEngine cc) throw new Exception("core not found");
        var xScaler = new Scaler(cc.DrawMarginLocation, cc.DrawMarginSize, cc.XAxes[xAxisIndex]);
        var yScaler = new Scaler(cc.DrawMarginLocation, cc.DrawMarginSize, cc.YAxes[yAxisIndex]);

        return new LvcPointD { X = xScaler.ToChartValues(point.X), Y = yScaler.ToChartValues(point.Y) };
    }

    /// <inheritdoc cref="ICartesianChartView.ScaleDataToPixels(LvcPointD, int, int)"/>
    public LvcPointD ScaleDataToPixels(LvcPointD point, int xAxisIndex = 0, int yAxisIndex = 0)
    {
        if (_core is not CartesianChartEngine cc) throw new Exception("core not found");

        var xScaler = new Scaler(cc.DrawMarginLocation, cc.DrawMarginSize, cc.XAxes[xAxisIndex]);
        var yScaler = new Scaler(cc.DrawMarginLocation, cc.DrawMarginSize, cc.YAxes[yAxisIndex]);

        return new LvcPointD { X = xScaler.ToPixels(point.X), Y = yScaler.ToPixels(point.Y) };
    }

    /// <inheritdoc cref="IChartView.GetPointsAt(LvcPointD, FindingStrategy, FindPointFor)"/>
    public IEnumerable<ChartPoint> GetPointsAt(LvcPointD point, FindingStrategy strategy = FindingStrategy.Automatic, FindPointFor findPointFor = FindPointFor.HoverEvent)
    {
        if (_core is not CartesianChartEngine cc) throw new Exception("core not found");

        if (strategy == FindingStrategy.Automatic)
            strategy = cc.Series.GetFindingStrategy();

        return cc.Series.SelectMany(series => series.FindHitPoints(cc, new(point), strategy, findPointFor));
    }

    /// <inheritdoc cref="IChartView.GetVisualsAt(LvcPointD)"/>
    public IEnumerable<IChartElement> GetVisualsAt(LvcPointD point)
    {
        return _core is not CartesianChartEngine cc
            ? throw new Exception("core not found")
            : cc.VisualElements.SelectMany(visual => ((VisualElement)visual).IsHitBy(_core, new(point)));
    }

    void IChartView.InvokeOnUIThread(Action action)
    {
        _ = DispatcherQueue.TryEnqueue(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () => action());
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LiveCharts.Configure(config => config.UseDefaults());

        if (_core is null)
        {
            _core = new CartesianChartEngine(
                this, config => config.UseDefaults(), _canvas.CanvasCore);

            OnMatchAxesScreenDataRatioChanged();

            if (SyncContext != null)
                _canvas.CanvasCore.Sync = SyncContext;

            if (_core == null) throw new Exception("Core not found!");
            _core.Measuring += OnCoreMeasuring;
            _core.UpdateStarted += OnCoreUpdateStarted;
            _core.UpdateFinished += OnCoreUpdateFinished;

            SizeChanged += OnSizeChanged;

            // We use the behaviours assembly to share to support Uno.WinUI
            var chartBehaviour = new ChartBehaviour();

            chartBehaviour.Pressed += OnPressed;
            chartBehaviour.Moved += OnMoved;
            chartBehaviour.Released += OnReleased;
            chartBehaviour.Scrolled += OnScrolled;
            chartBehaviour.Pinched += OnPinched;
            chartBehaviour.Exited += OnExited;

            chartBehaviour.On(this);
        }

        _core.Load();
        _core.Update();

        _themeListener = new(CoreChart.ApplyTheme, DispatcherQueue);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_core == null) throw new Exception("Core not found!");
        _core.Update();
    }

    private void OnPressed(object? sender, Behaviours.Events.PressedEventArgs args)
    {
        // is this working on all platforms?
        //if (args.KeyModifiers > 0) return;

        var cArgs = new PointerCommandArgs(this, new(args.Location.X, args.Location.Y), args);
        if (PointerPressedCommand?.CanExecute(cArgs) == true) PointerPressedCommand.Execute(cArgs);

        _core?.InvokePointerDown(args.Location, args.IsSecondaryPress);
    }

    private void OnMoved(object? sender, Behaviours.Events.ScreenEventArgs args)
    {
        var location = args.Location;

        var cArgs = new PointerCommandArgs(this, new(location.X, location.Y), args.OriginalEvent);
        if (PointerMoveCommand?.CanExecute(cArgs) == true) PointerMoveCommand.Execute(cArgs);

        _core?.InvokePointerMove(location);
    }

    private void OnReleased(object? sender, Behaviours.Events.PressedEventArgs args)
    {
        var cArgs = new PointerCommandArgs(this, new(args.Location.X, args.Location.Y), args);
        if (PointerReleasedCommand?.CanExecute(cArgs) == true) PointerReleasedCommand.Execute(cArgs);

        _core?.InvokePointerUp(args.Location, args.IsSecondaryPress);
    }

    private void OnScrolled(object? sender, Behaviours.Events.ScrollEventArgs args)
    {
        if (_core is null) throw new Exception("core not found");
        var c = (CartesianChartEngine)_core;
        c.Zoom(args.Location, args.ScrollDelta > 0 ? ZoomDirection.ZoomIn : ZoomDirection.ZoomOut);
    }

    private void OnPinched(object? sender, Behaviours.Events.PinchEventArgs args)
    {
        if (_core is null) return;

        var c = (CartesianChartEngine)_core;
        var p = args.PinchStart;
        var s = c.ControlSize;
        var pivot = new LvcPoint((float)(p.X * s.Width), (float)(p.Y * s.Height));
        c.Zoom(pivot, ZoomDirection.DefinedByScaleFactor, args.Scale, true);
    }

    private void OnExited(object? sender, Behaviours.Events.EventArgs args) =>
        _core?.InvokePointerLeft();

    private void OnCoreUpdateFinished(IChartView chart) =>
        UpdateFinished?.Invoke(this);

    private void OnCoreUpdateStarted(IChartView chart)
    {
        if (UpdateStartedCommand is not null)
        {
            var args = new ChartCommandArgs(this);
            if (UpdateStartedCommand.CanExecute(args)) UpdateStartedCommand.Execute(args);
        }

        UpdateStarted?.Invoke(this);
    }

    private void OnCoreMeasuring(IChartView chart) =>
        Measuring?.Invoke(this);

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _core?.Unload();
        _observe.Dispose();
        _themeListener?.Dispose();
        _themeListener = null;
    }

    private void OnMatchAxesScreenDataRatioChanged()
    {
        if (_core is null) return;
        if (MatchAxesScreenDataRatio) SharedAxes.MatchAxesScreenDataRatio(this);
        else SharedAxes.DisposeMatchAxesScreenDataRatio(this);
    }

    private static void OnDependencyPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
    {
        var chart = (CartesianChart)o;
        if (chart._core == null) return;

        chart._core.Update();
    }

    void IChartView.OnDataPointerDown(IEnumerable<ChartPoint> points, LvcPoint pointer)
    {
        DataPointerDown?.Invoke(this, points);
        if (DataPointerDownCommand is not null && DataPointerDownCommand.CanExecute(points)) DataPointerDownCommand.Execute(points);

        ChartPointPointerDown?.Invoke(this, points.FindClosestTo(pointer));
#pragma warning disable CS0618 // Type or member is obsolete
        ChartPointPointerDownCommand?.Execute(points.FindClosestTo(pointer));
#pragma warning restore CS0618 // Type or member is obsolete
    }

    void IChartView.OnHoveredPointsChanged(IEnumerable<ChartPoint>? newPoints, IEnumerable<ChartPoint>? oldPoints)
    {
        HoveredPointsChanged?.Invoke(this, newPoints, oldPoints);

        var args = new HoverCommandArgs(this, newPoints, oldPoints);
        if (HoveredPointsChangedCommand is not null && HoveredPointsChangedCommand.CanExecute(args)) HoveredPointsChangedCommand.Execute(args);
    }

    void IChartView.OnVisualElementPointerDown(
        IEnumerable<IInteractable> visualElements, LvcPoint pointer)
    {
        var args = new VisualElementsEventArgs(CoreChart, visualElements, pointer);

        VisualElementsPointerDown?.Invoke(this, args);
        if (VisualElementsPointerDownCommand is not null && VisualElementsPointerDownCommand.CanExecute(args))
            VisualElementsPointerDownCommand.Execute(args);
    }

    void IChartView.Invalidate() =>
        CoreCanvas.Invalidate();

    private static void OnSeriesSourceChanged(CartesianChart chart, object o, object n)
    {
        var seriesObserver = (SeriesSourceObserver)chart._observe[nameof(SeriesSource)];
        seriesObserver.Initialize(chart.SeriesSource);

        if (seriesObserver.Series is not null)
            chart.Series = seriesObserver.Series;
    }

    private static PropertyChangedCallback InitializeObserver(string propertyName) =>
        (o, args) =>
            ((CartesianChart)o)._observe[propertyName].Initialize(args.NewValue);

    private void AddUIElement(object item)
    {
        if (_canvas is null || item is not UIElement uiElement) return;
        _canvas.Children.Add(uiElement);
    }

    private void RemoveUIElement(object item)
    {
        if (_canvas is null || item is not UIElement uiElement) return;
        _ = _canvas.Children.Remove(uiElement);
    }

    private ISeries InflateSeriesTemplate(object item)
    {
        var content = (FrameworkElement)SeriesTemplate.LoadContent();

        if (content is not ISeries series)
            throw new InvalidOperationException("The template must be a valid series.");

        content.DataContext = item;

        return series;
    }

    private static object GetSeriesSource(ISeries series) =>
        ((FrameworkElement)series).DataContext!;
}
