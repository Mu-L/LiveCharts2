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
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Motion;
using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace LiveChartsGeneratedCode;

// ==============================================================================
// 
// this file contains the MAUI specific code for the SourceGenChart class,
// the rest of the code can be found in the _Shared project.
// 
// ==============================================================================

/// <inheritdoc cref="IChartView"/>
public abstract partial class SourceGenChart : ChartView, IChartView
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceGenChart"/> class.
    /// </summary>
    protected SourceGenChart()
    {
        Content = new MotionCanvas(ForceGPU);

        SizeChanged += (s, e) =>
            CoreChart.Update();

        InitializeChartControl();
        InitializeObservedProperties();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        if (Application.Current is not null)
            Application.Current.RequestedThemeChanged += (sender, args) => CoreChart?.ApplyTheme();
    }

    private MotionCanvas CanvasView => (MotionCanvas)Content;

    /// <inheritdoc cref="IChartView.CoreCanvas"/>
    public CoreMotionCanvas CoreCanvas => CanvasView.CanvasCore;

    bool IChartView.DesignerMode => false;
    bool IChartView.IsDarkMode => Application.Current?.RequestedTheme == AppTheme.Dark;
    LvcColor IChartView.BackColor
    {
        get
        {
            var c = (Background as SolidColorBrush)?.Color ?? BackgroundColor;
            return c is not null
                ? LvcColor.FromArgb((byte)(c.Alpha * 255), (byte)(c.Red * 255), (byte)(c.Green * 255), (byte)(c.Blue * 255))
                : CoreCanvas._virtualBackgroundColor;
        }
    }
    LvcSize IChartView.ControlSize => new() { Width = (float)Width, Height = (float)Height };

    private void OnLoaded(object? sender, EventArgs e)
    {
        StartObserving();
        CoreChart?.Load();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        StopObserving();
        CoreChart?.Unload();
    }

    private ISeries InflateSeriesTemplate(object item)
    {
        if (SeriesTemplate.CreateContent() is not View template)
            throw new InvalidOperationException("The template must be a View.");
        if (template is not ISeries series)
            throw new InvalidOperationException("The template is not a valid series.");

        template.BindingContext = item;

        return series;
    }

    private static object GetSeriesSource(ISeries series) => ((View)series).BindingContext;

    private void AddUIElement(object item)
    {
        if (item is not View view) return;
        CanvasView.Children.Add(view);
    }

    private void RemoveUIElement(object item)
    {
        if (item is not View view) return;
        _ = CanvasView.Children.Remove(view);
    }

    internal override void OnPressed(object? sender, LiveChartsCore.Native.Events.PressedEventArgs args)
    {
        // not implemented yet?
        // https://github.com/dotnet/maui/issues/16202
        //if (Keyboard.Modifiers > 0) return;

        var cArgs = new PointerCommandArgs(this, new(args.Location.X, args.Location.Y), args);
        if (PressedCommand?.CanExecute(cArgs) == true)
            PressedCommand.Execute(cArgs);

        CoreChart.InvokePointerDown(args.Location, args.IsSecondaryPress);
    }

    internal override void OnMoved(object? sender, LiveChartsCore.Native.Events.ScreenEventArgs args)
    {
        var location = args.Location;

        var cArgs = new PointerCommandArgs(this, new(location.X, location.Y), args.OriginalEvent);
        if (MovedCommand?.CanExecute(cArgs) == true)
            MovedCommand.Execute(cArgs);

        CoreChart.InvokePointerMove(location);
    }

    internal override void OnReleased(object? sender, LiveChartsCore.Native.Events.PressedEventArgs args)
    {
        var cArgs = new PointerCommandArgs(this, new(args.Location.X, args.Location.Y), args);
        if (ReleasedCommand?.CanExecute(cArgs) == true)
            ReleasedCommand.Execute(cArgs);

        CoreChart.InvokePointerUp(args.Location, args.IsSecondaryPress);
    }

    internal override void OnExited(object? sender, LiveChartsCore.Native.Events.EventArgs args) =>
        CoreChart.InvokePointerLeft();

    void IChartView.InvokeOnUIThread(Action action) =>
        MainThread.BeginInvokeOnMainThread(action);
}
