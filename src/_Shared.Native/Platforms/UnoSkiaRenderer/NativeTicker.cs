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

#if !HAS_OS_LVC && UNO_LVC

// reachable on uno skia renderer
// HAS_OS_LVC is true when the target framework contains any of the following:
// -windows, -android, -ios, -maccatalyst, -tize
// currently this is the the same file as WinUI, because uno makes this work across platforms
// but by design this file is separated so in the future if there are any uno specific changes

using LiveChartsCore.Motion;
using Microsoft.UI.Xaml.Media;

namespace LiveChartsCore.Native;

internal partial class NativeFrameTicker : IFrameTicker
{
    private IRenderMode _renderMode = null!;
    private CoreMotionCanvas _canvas = null!;

    public void InitializeTicker(CoreMotionCanvas canvas, IRenderMode renderMode)
    {
        _canvas = canvas;
        _renderMode = renderMode;

        _canvas.Invalidated += OnCoreInvalidated;
        CompositionTarget.Rendering += OnCompositonTargetRendering;

        CoreMotionCanvas.s_tickerName = "CompositionTarget.Rendering WinUI";
    }

    private void OnCoreInvalidated(CoreMotionCanvas obj) =>
        _renderMode.InvalidateRenderer();

    private void OnCompositonTargetRendering(object? sender, object e)
    {
        if (_canvas.IsValid) return;
        _renderMode.InvalidateRenderer();
    }

    public void DisposeTicker()
    {
        CompositionTarget.Rendering -= OnCompositonTargetRendering;
        _canvas?.Invalidated -= OnCoreInvalidated;

        _canvas = null!;
        _renderMode = null!;
    }
}

#endif
