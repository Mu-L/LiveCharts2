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
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiveChartsCore.Drawing;
using LiveChartsCore.Motion;
using LiveChartsCore.SkiaSharpView.Drawing;
using SkiaSharp.Views.Desktop;

namespace LiveChartsCore.SkiaSharpView.WinForms;

/// <summary>
/// The motion canvas control for windows forms, <see cref="CoreMotionCanvas"/>.
/// </summary>
/// <seealso cref="UserControl" />
public partial class MotionCanvas : UserControl
{
    private bool _isDrawingLoopRunning = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="MotionCanvas"/> class.
    /// </summary>
    public MotionCanvas()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the canvas core.
    /// </summary>
    /// <value>
    /// The canvas core.
    /// </value>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CoreMotionCanvas CanvasCore { get; } = new();

    /// <inheritdoc cref="Control.CreateHandle()"/>
    protected override void CreateHandle()
    {
        base.CreateHandle();
        CanvasCore.Invalidated += CanvasCore_Invalidated;
    }

    /// <inheritdoc cref="Control.OnHandleDestroyed(EventArgs)"/>
    protected override void OnHandleDestroyed(EventArgs e)
    {
        base.OnHandleDestroyed(e);

        CanvasCore.Invalidated -= CanvasCore_Invalidated;
        CanvasCore.Dispose();
    }

    private void SkControl_PaintSurface(object sender, SKPaintSurfaceEventArgs e) =>
        CanvasCore.DrawFrame(
            new SkiaSharpDrawingContext(CanvasCore, e.Info, e.Surface, GetBackground().AsSKColor()));

    private void SkglControl_PaintSurface(object sender, SKPaintGLSurfaceEventArgs e) =>
        CanvasCore.DrawFrame(
            new SkiaSharpDrawingContext(CanvasCore, e.Info, e.Surface, GetBackground().AsSKColor()));

    private void CanvasCore_Invalidated(CoreMotionCanvas sender) =>
        RunDrawingLoop();

    private async void RunDrawingLoop()
    {
        if (_isDrawingLoopRunning) return;
        _isDrawingLoopRunning = true;

        var ts = TimeSpan.FromSeconds(1 / LiveCharts.TargetFps);

        while (!CanvasCore.IsValid)
        {
            _skControl?.Invalidate();
            _skglControl?.Invalidate();

            await Task.Delay(ts);
        }

        _isDrawingLoopRunning = false;
    }

    private LvcColor GetBackground() =>
        true
            ? new LvcColor(Parent!.BackColor.R, Parent.BackColor.G, Parent.BackColor.B)
            : CanvasCore._virtualBackgroundColor; // are themes relevant in  Win Forms?
}
