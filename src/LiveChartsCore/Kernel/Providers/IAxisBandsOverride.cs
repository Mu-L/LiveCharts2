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
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Painting;

namespace LiveChartsCore.Kernel.Providers;

/// <summary>
/// An optional capability of an axis render override (the object returned by
/// <see cref="ChartEngine.GetAxisRenderOverride(ICartesianAxis)"/>): when the override also
/// implements this interface, the axis draws the bands it supplies as rectangles in the draw
/// margin, behind the series — e.g. alternating (zebra) bands following the axis separators.
/// The axis owns the geometry lifecycle (animation on zoom/pan, fade in/out, clipping); the
/// override only decides which ranges to fill and with which paint.
/// </summary>
public interface IAxisBandsOverride
{
    /// <summary>
    /// Creates a new rectangle geometry for one band; called once per band that enters the
    /// visible range (the axis caches and animates the instances).
    /// </summary>
    BoundedDrawnGeometry CreateBandGeometry();

    /// <summary>
    /// Supplies the bands to fill for the current visible range, in axis units. Bands may
    /// extend past <paramref name="min"/>/<paramref name="max"/> — the draw margin clips them.
    /// Called while measuring, so it must not mutate the axis (a property change would
    /// re-trigger the measure). Return <see langword="false"/> to draw no bands.
    /// </summary>
    /// <param name="axis">The axis being measured.</param>
    /// <param name="chart">The chart being measured.</param>
    /// <param name="min">The effective visible minimum, in axis units.</param>
    /// <param name="max">The effective visible maximum, in axis units.</param>
    /// <param name="separators">The separator values the axis is drawing this measure, in axis
    /// units, ordered ascending; grouped axes (see <see cref="IAxisRenderOverride"/>) report
    /// their grouped separators here.</param>
    /// <param name="bands">The bands to fill, or null.</param>
    /// <param name="paint">The paint to fill the bands with, or null.</param>
    bool TryGetBands(
        ICartesianAxis axis,
        Chart chart,
        double min,
        double max,
        IReadOnlyList<double> separators,
        out IEnumerable<AxisBand>? bands,
        out Paint? paint);
}
