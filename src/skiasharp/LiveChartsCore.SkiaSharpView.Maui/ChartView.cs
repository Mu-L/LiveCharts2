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
using Microsoft.Maui.Controls;

namespace LiveChartsCore.SkiaSharpView.Maui;

/// <summary>
/// Base class for views that display a chart.
/// </summary>
public abstract class ChartView : ContentView
{
    static ChartView()
    {
        if (!LiveChartsCoreMauiAppBuilderExtensions.AreHandlersRegistered)
        {
            throw new InvalidOperationException(
                "Since rc5 version, `.UseLiveCharts()` and `.UseSkiaSharp()` must be " +
                "chained to `.UseMauiApp<T>()`, in the MauiProgram.cs file. For more info see:" +
                "https://livecharts.dev/docs/Maui/2.0.0-rc5/Overview.Installation");
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartView"/> class.
    /// </summary>
    protected ChartView()
    {
        Content = new MotionCanvas();
    }

    /// <summary>
    /// Gets the canvas view.
    /// </summary>
    public MotionCanvas CanvasView => (MotionCanvas)Content;

    /// <summary>
    /// Gets the core chart.
    /// </summary>
    public abstract Chart CoreChart { get; }

    internal virtual void OnPressed(object? sender, Behaviours.Events.PressedEventArgs args) { }
    internal virtual void OnMoved(object? sender, Behaviours.Events.ScreenEventArgs args) { }
    internal virtual void OnReleased(object? sender, Behaviours.Events.PressedEventArgs args) { }
    internal virtual void OnScrolled(object? sender, Behaviours.Events.ScrollEventArgs args) { }
    internal virtual void OnPinched(object? sender, Behaviours.Events.PinchEventArgs args) { }
    internal virtual void OnExited(object? sender, Behaviours.Events.EventArgs args) { }
}
