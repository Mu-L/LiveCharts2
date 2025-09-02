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

using LiveChartsCore.Kernel;

namespace LiveChartsCore.Motion;

/// <summary>
/// Defines a motion canvas composer, a pair of render mode and frame ticker.
/// </summary>
public class MotionCanvasComposer(IRenderMode renderMode, IFrameTicker ticker)
{
    /// <summary>
    /// Defines the motion canvas rendering factory delegate.
    /// </summary>
    /// <param name="settings">The global rendering settings.</param>
    /// <param name="forceGPU">Indicates whether GPU mode is forced.</param>
    /// <returns>A MotionCanvasComposer instance.</returns>
    public delegate MotionCanvasComposer MotionCanvasRenderingFactoryDelegate(RenderingSettings settings, bool forceGPU);

    /// <summary>
    /// Gets the render mode.
    /// </summary>
    public IRenderMode RenderMode { get; } = renderMode;

    /// <summary>
    /// Gets the ticker.
    /// </summary>
    public IFrameTicker Ticker { get; } = ticker;

    /// <summary>
    /// Initializes the composer.
    /// </summary>
    /// <param name="canvas"></param>
    public void Initialize(CoreMotionCanvas canvas)
    {
        RenderMode.InitializeRenderMode(canvas);
        Ticker.InitializeTicker(canvas, RenderMode);
        RenderMode.FrameRequest += canvas.DrawFrame;
    }

    /// <summary>
    /// Disposes the composer.
    /// </summary>
    /// <param name="canvas"></param>
    public void Dispose(CoreMotionCanvas canvas)
    {
        RenderMode.DisposeRenderMode();
        Ticker.DisposeTicker();
        RenderMode.FrameRequest -= canvas.DrawFrame;
        canvas.Dispose();
    }
}
