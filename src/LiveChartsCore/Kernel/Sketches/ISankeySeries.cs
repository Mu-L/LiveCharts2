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

namespace LiveChartsCore.Kernel.Sketches;

/// <summary>
/// Common (non-typed) surface of a sankey series. Graph topology is provided
/// via series-level mappers exposed on the typed
/// <c>CoreSankeySeries&lt;TNode, TVisual, TLabel&gt;</c>.
/// </summary>
public interface ISankeySeries : ISeries, IStrokedAndFilled
{
    /// <summary>Width (px) reserved for each node's column rectangle. Default 12.</summary>
    double NodeWidth { get; set; }

    /// <summary>Vertical gap (px) between sibling nodes within the same column. Default 8.</summary>
    double NodePadding { get; set; }

    /// <summary>
    /// Corner radius (px) applied to node rectangles when the visual is a
    /// rounded-rectangle geometry. Default 0 — matches the typical d3-sankey
    /// sharp-corner aesthetic. Ignored when the user picks a custom TVisual
    /// that doesn't derive from <c>BaseRoundedRectangleGeometry</c>.
    /// </summary>
    double NodeCornerRadius { get; set; }

    /// <summary>
    /// Number of relaxation iterations used by the d3-sankey layout to
    /// minimize ribbon crossings. Higher = cleaner but slower. Default 32.
    /// </summary>
    int LayoutIterations { get; set; }

    /// <summary>
    /// Alpha (0..1) the link fill is multiplied by when rendering the ribbons
    /// (since the ribbon fill is typically a tinted version of the node fill).
    /// Default 0.5.
    /// </summary>
    double LinkOpacity { get; set; }
}
