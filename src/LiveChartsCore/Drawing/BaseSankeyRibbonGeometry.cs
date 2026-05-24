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

using LiveChartsCore.Generators;

namespace LiveChartsCore.Drawing;

/// <summary>
/// A sankey flow ribbon — a closed Bezier band connecting the right edge of a
/// source node to the left edge of a target node. Six animatable endpoints
/// (X + top-Y + bottom-Y for each end) drive the path; the platform-specific
/// implementation builds two cubic curves with control points at the
/// horizontal midpoint between source and target.
/// </summary>
public abstract partial class BaseSankeyRibbonGeometry : DrawnGeometry
{
    /// <summary>X coordinate of the source connection (right edge of source node).</summary>
    [MotionProperty]
    public partial float SourceX { get; set; }

    /// <summary>Top Y of the source connection band.</summary>
    [MotionProperty]
    public partial float SourceY0 { get; set; }

    /// <summary>Bottom Y of the source connection band.</summary>
    [MotionProperty]
    public partial float SourceY1 { get; set; }

    /// <summary>X coordinate of the target connection (left edge of target node).</summary>
    [MotionProperty]
    public partial float TargetX { get; set; }

    /// <summary>Top Y of the target connection band.</summary>
    [MotionProperty]
    public partial float TargetY0 { get; set; }

    /// <summary>Bottom Y of the target connection band.</summary>
    [MotionProperty]
    public partial float TargetY1 { get; set; }
}
