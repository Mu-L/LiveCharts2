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

using LiveChartsCore.Motion;
using LiveChartsCore.SkiaSharpView.Drawing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SkiaSharp.Views.Blazor;

namespace LiveChartsCore.SkiaSharpView.Blazor;

/// <inheritdoc cref="CoreMotionCanvas"/>
public partial class MotionCanvas : IDisposable
{
    private SKGLView? _glView;
    private SKCanvasView? _canvas;
    private bool _disposing = false;
    private bool _isDrawingLoopRunning = false;

    /// <summary>
    /// Called when the control is initialized.
    /// </summary>
    protected override void OnInitialized() =>
        CanvasCore.Invalidated += CanvasCore_Invalidated;

    /// <summary>
    /// Gets the <see cref="CoreMotionCanvas"/> (core).
    /// </summary>
    public CoreMotionCanvas CanvasCore { get; } = new();

    /// <summary>
    /// Gets or sets the pointer down callback.
    /// </summary>
    [Parameter]
    public EventCallback<PointerEventArgs> OnPointerDownCallback { get; set; }

    /// <summary>
    /// Gets or sets the pointer move callback.
    /// </summary>
    [Parameter]
    public EventCallback<PointerEventArgs> OnPointerMoveCallback { get; set; }

    /// <summary>
    /// Gets or sets the pointer up callback.
    /// </summary>
    [Parameter]
    public EventCallback<PointerEventArgs> OnPointerUpCallback { get; set; }

    /// <summary>
    /// Gets or sets the wheel changed callback.
    /// </summary>
    [Parameter]
    public EventCallback<WheelEventArgs> OnWheelCallback { get; set; }

    /// <summary>
    /// Gets or sets the pointer leave callback.
    /// </summary>
    [Parameter]
    public EventCallback<PointerEventArgs> OnPointerOutCallback { get; set; }

    /// <summary>
    /// Called when the pointer goes down.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnPointerDown(PointerEventArgs e) =>
        _ = OnPointerDownCallback.InvokeAsync(e);

    /// <summary>
    /// Called when the pointer moves.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnPointerMove(PointerEventArgs e) =>
        _ = OnPointerMoveCallback.InvokeAsync(e);

    /// <summary>
    /// Called when the pointer goes up.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnPointerUp(PointerEventArgs e) =>
        _ = OnPointerUpCallback.InvokeAsync(e);

    /// <summary>
    /// Called when the wheel moves.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnWheel(WheelEventArgs e) => _ = OnWheelCallback.InvokeAsync(e);

    /// <summary>
    /// Called when the pointer leaves the control.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnPointerOut(PointerEventArgs e) =>
        _ = OnPointerOutCallback.InvokeAsync(e);

    private void OnPaintGlSurface(SKPaintGLSurfaceEventArgs e) =>
        CanvasCore.DrawFrame(new SkiaSharpDrawingContext(CanvasCore, e.Info, e.Surface, e.Surface.Canvas));

    private void OnPaintSurface(SKPaintSurfaceEventArgs e) =>
        CanvasCore.DrawFrame(new SkiaSharpDrawingContext(CanvasCore, e.Info, e.Surface, e.Surface.Canvas));

    private void CanvasCore_Invalidated(CoreMotionCanvas sender) =>
        RunDrawingLoop();

    private async void RunDrawingLoop()
    {
        if (_isDrawingLoopRunning) return;
        _isDrawingLoopRunning = true;

        var ts = TimeSpan.FromSeconds(1 / LiveCharts.MaxFps);

        if (LiveCharts.UseGPU)
        {
            while (!CanvasCore.IsValid && !_disposing)
            {
#pragma warning disable CA1416 // Validate platform compatibility
                _glView?.Invalidate();
#pragma warning restore CA1416 // Validate platform compatibility
                await Task.Delay(ts);
            }
        }
        else
        {
            while (!CanvasCore.IsValid && !_disposing)
            {
#pragma warning disable CA1416 // Validate platform compatibility
                _canvas?.Invalidate();
#pragma warning restore CA1416 // Validate platform compatibility
                await Task.Delay(ts);
            }
        }

        _isDrawingLoopRunning = false;
    }

    void IDisposable.Dispose() => _disposing = true;
}
