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

using System.Collections.Generic;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;

namespace LiveChartsCore.SkiaSharpView;

/// <summary>Defines a sankey series in the user interface.</summary>
public class SankeySeries<TNode>
    : SankeySeries<TNode, RoundedRectangleGeometry, LabelGeometry>
    where TNode : notnull
{
    /// <summary>Initializes a new instance of the <see cref="SankeySeries{TNode}"/> class.</summary>
    public SankeySeries() : base() { }

    /// <summary>Initializes a new instance with the given nodes.</summary>
    public SankeySeries(IReadOnlyCollection<TNode>? nodes) : base(nodes) { }
}

/// <summary>Defines a sankey series in the user interface with a custom node visual.</summary>
public class SankeySeries<TNode, TVisual>
    : SankeySeries<TNode, TVisual, LabelGeometry>
    where TNode : notnull
    where TVisual : BoundedDrawnGeometry, new()
{
    /// <summary>Initializes a new instance of the <see cref="SankeySeries{TNode, TVisual}"/> class.</summary>
    public SankeySeries() : base() { }

    /// <summary>Initializes a new instance with the given nodes.</summary>
    public SankeySeries(IReadOnlyCollection<TNode>? nodes) : base(nodes) { }
}

/// <summary>Defines a sankey series in the user interface with custom node visual and label.</summary>
public class SankeySeries<TNode, TVisual, TLabel>
    : CoreSankeySeries<TNode, TVisual, TLabel>
    where TNode : notnull
    where TVisual : BoundedDrawnGeometry, new()
    where TLabel : BaseLabelGeometry, new()
{
    static SankeySeries() => LiveChartsSkiaSharp.EnsureInitialized();

    /// <summary>Initializes a new instance of the <see cref="SankeySeries{TNode, TVisual, TLabel}"/> class.</summary>
    public SankeySeries() : base(null) { }

    /// <summary>Initializes a new instance with the given nodes.</summary>
    public SankeySeries(IReadOnlyCollection<TNode>? nodes) : base(nodes) { }

    /// <inheritdoc cref="CoreSankeySeries{TNode, TVisual, TLabel}.CreateRibbonVisual"/>
    protected override BaseSankeyRibbonGeometry CreateRibbonVisual() => new SankeyRibbonGeometry();
}
