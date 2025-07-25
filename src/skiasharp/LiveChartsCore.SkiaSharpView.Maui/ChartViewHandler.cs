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

#if IOS || MACCATALYST
using PlatformView = Microsoft.Maui.Platform.ContentView;
#elif ANDROID
using PlatformView = Microsoft.Maui.Platform.ContentViewGroup;
#elif WINDOWS
using PlatformView = Microsoft.Maui.Platform.ContentPanel;
#else
using PlatformView = System.Object;
#endif

using LiveChartsCore.Behaviours.Events;
using Microsoft.Maui.Handlers;

namespace LiveChartsCore.SkiaSharpView.Maui;

/// <summary>
/// A <see cref="ContentViewHandler"/> specific to the <see cref="ChartView"/> control.
/// </summary>
public class ChartViewHandler : ContentViewHandler
{
    private readonly PointerController _chartBehaviour;

    private ChartView? ChartView => VirtualView as ChartView;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartViewHandler"/> class.
    /// </summary>
    public ChartViewHandler()
    {
        _chartBehaviour = new PointerController();
        _chartBehaviour.Pressed += OnPressed;
        _chartBehaviour.Moved += OnMoved;
        _chartBehaviour.Released += OnReleased;
        _chartBehaviour.Scrolled += OnScrolled;
        _chartBehaviour.Pinched += OnPinched;
        _chartBehaviour.Exited += OnExited;
    }

    /// <inheritdoc />
    protected override void ConnectHandler(PlatformView platformView)
    {
        base.ConnectHandler(platformView);
        _chartBehaviour.On(platformView);
    }

    /// <inheritdoc />
    protected override void DisconnectHandler(PlatformView platformView)
    {
        _chartBehaviour.Off(platformView);
        base.DisconnectHandler(platformView);
    }

    private void OnPressed(object? sender, PressedEventArgs args) => ChartView?.OnPressed(sender, args);
    private void OnMoved(object? sender, ScreenEventArgs args) => ChartView?.OnMoved(sender, args);
    private void OnReleased(object? sender, PressedEventArgs args) => ChartView?.OnReleased(sender, args);
    private void OnScrolled(object? sender, ScrollEventArgs args) => ChartView?.OnScrolled(sender, args);
    private void OnPinched(object? sender, PinchEventArgs args) => ChartView?.OnPinched(sender, args);
    private void OnExited(object? sender, EventArgs args) => ChartView?.OnExited(sender, args);
}
