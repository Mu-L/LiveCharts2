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

using System.Windows;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Motion;
using LiveChartsCore.SkiaSharpView.Drawing;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace LiveChartsCore.SkiaSharpView.WPF.Rendering;

internal class CPURenderMode : SKElement, IRenderMode
{
    private CoreMotionCanvas _canvas = null!;

    public event CoreMotionCanvas.FrameRequestHandler? FrameRequest;

    public void InitializeRenderMode(CoreMotionCanvas canvas)
    {
        _canvas = canvas;
        PaintSurface += OnPaintSurface;

        CoreMotionCanvas.s_rendererName = $"{nameof(CPURenderMode)} and {nameof(SKElement)}";
    }

    public void DisposeRenderMode()
    {
        _canvas = null!;
        PaintSurface -= OnPaintSurface;
    }

    public void InvalidateRenderer() =>
        InvalidateVisual();

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs args)
    {
        var density = GetPixelDensity();
        if (density.dpix != 1 || density.dpiy != 1)
            args.Surface.Canvas.Scale(density.dpix, density.dpiy);

        FrameRequest?.Invoke(
            new SkiaSharpDrawingContext(_canvas, args.Surface.Canvas, GetBackground()));
    }

    private ResolutionHelper GetPixelDensity()
    {
        var presentationSource = PresentationSource.FromVisual(this);
        if (presentationSource is null) return new(1f, 1f);
        var compositionTarget = presentationSource.CompositionTarget;
        if (compositionTarget is null) return new(1f, 1f);

        var matrix = compositionTarget.TransformToDevice;
        return new((float)matrix.M11, (float)matrix.M22);
    }

    private SKColor GetBackground() =>
        ((Parent as FrameworkElement)?.Parent as IChartView)?.BackColor.AsSKColor() ?? SKColor.Empty;
}
