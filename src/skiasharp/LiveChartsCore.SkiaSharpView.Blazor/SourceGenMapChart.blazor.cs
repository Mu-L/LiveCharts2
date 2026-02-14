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

using LiveChartsCore.Drawing;
using LiveChartsCore.Geo;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Motion;
using LiveChartsCore.SkiaSharpView.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace LiveChartsGeneratedCode;

// ===============================================
// this file contains the Blazor specific code
// ===============================================

/// <inheritdoc cref="IChartView" />
public abstract partial class SourceGenMapChart : ComponentBase, IDisposable, IGeoMapView
{
#pragma warning disable IDE0032 // Use auto property, blazor ref
    private MotionCanvas _motionCanvas = null!;
#pragma warning restore IDE0032 // Use auto property

    /// <summary>
    /// Initializes a new instance of the <see cref="SourceGenMapChart"/> class.
    /// </summary>
    protected SourceGenMapChart()
    {
        CoreChart = null!;
    }

    /// <inheritdoc cref="IDrawnView.CoreCanvas"/>
    public CoreMotionCanvas CoreCanvas => _motionCanvas.CanvasCore;

    bool IGeoMapView.DesignerMode => false;

    LvcSize IDrawnView.ControlSize => new()
    {
        Width = _motionCanvas.Width,
        Height = _motionCanvas.Height
    };

    /// <summary>
    /// Builds the render tree.
    /// </summary>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<MotionCanvas>(0);
        builder.AddComponentReferenceCapture(1, r => _motionCanvas = (MotionCanvas)r);
        builder.CloseComponent();
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender) return;

        InitializeChartControl();

        _motionCanvas.SizeChanged +=
            () =>
                CoreChart.Update();

        CoreCanvas.Sync = SyncContext;
    }

    void IGeoMapView.InvokeOnUIThread(Action action) =>
        _ = InvokeAsync(action);

    void IDisposable.Dispose() =>
        CoreChart.Unload();
}
