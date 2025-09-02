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
using Eto.Drawing;
using Eto.Forms;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Motion;
using LiveChartsCore.SkiaSharpView.Eto;

namespace LiveChartsGeneratedCode;

// ==============================================================================
// 
// this file contains the Eto specific code for the ChartControl class,
// the rest of the code can be found in the _Shared project.
// 
// ==============================================================================

/// <inheritdoc cref="IChartView" />
public abstract partial class ChartControl : Panel, IChartView
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChartControl"/> class.
    /// </summary>
    protected ChartControl()
    {
        var motionCanvas = new MotionCanvas();

        Content = motionCanvas;
        BackgroundColor = Colors.White;

        InitializeChartControl();
        InitializeObservedProperties();

        Content.SizeChanged += (s, e) =>
            CoreChart.Update();

        Content.MouseDown += OnMouseDown;
        Content.MouseMove += OnMouseMove;
        Content.MouseUp += OnMouseUp;
        Content.MouseLeave += OnMouseLeave;
    }

    /// <inheritdoc cref="IChartView.CoreCanvas"/>
    public CoreMotionCanvas CoreCanvas => ((MotionCanvas)Content).CanvasCore;

    bool IChartView.DesignerMode => false;

    bool IChartView.IsDarkMode => false;

    LvcColor IChartView.BackColor =>
        new((byte)BackgroundColor.Rb, (byte)BackgroundColor.Gb, (byte)BackgroundColor.Bb, (byte)BackgroundColor.Ab);

    LvcSize IChartView.ControlSize => new() { Width = Content.Width, Height = Content.Height };

    void IChartView.InvokeOnUIThread(Action action) =>
        _ = Application.Instance.InvokeAsync(action);

    /// <inheritdoc cref="Control.OnLoadComplete(EventArgs)"/>
    protected override void OnLoadComplete(EventArgs e)
    {
        base.OnLoadComplete(e);
        StartObserving();
        CoreChart.Load();
    }

    /// <inheritdoc cref="Control.OnUnLoad(EventArgs)"/>
    protected override void OnUnLoad(EventArgs e)
    {
        base.OnUnLoad(e);
        StopObserving();
        CoreChart.Unload();
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        base.OnMouseMove(e);
        CoreChart?.InvokePointerMove(new LvcPoint(e.Location.X, e.Location.Y));
    }

    private void OnMouseDown(object? sender, MouseEventArgs e) =>
        //if (ModifierKeys > 0) return; // is this supported in Eto.Forms?
        CoreChart?.InvokePointerDown(new LvcPoint(e.Location.X, e.Location.Y), e.Buttons != MouseButtons.Primary);

    private void OnMouseUp(object? sender, MouseEventArgs e) =>
        CoreChart?.InvokePointerUp(new LvcPoint(e.Location.X, e.Location.Y), e.Buttons != MouseButtons.Primary);

    private void OnMouseLeave(object? sender, MouseEventArgs e) =>
        CoreChart?.InvokePointerLeft();
}
