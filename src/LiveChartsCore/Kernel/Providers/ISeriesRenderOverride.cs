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
using LiveChartsCore.Measure;

namespace LiveChartsCore.Kernel.Providers;

/// <summary>
/// An optional hook a <see cref="ChartEngine"/> can supply to take over the
/// rendering of a series, bypassing its per-point <see cref="IChartElement.Invalidate(Chart)"/>.
/// High-performance backends (e.g. a batched level-of-detail renderer) use this to
/// swap in a faster draw path with no user code change. The default OSS engine
/// returns none, so the series renders itself.
/// </summary>
/// <remarks>
/// Also implement <see cref="IReusesSeriesPaints"/> when the override draws through the overridden
/// series' OWN paint tasks (so its gradient/effects apply) and manages their cleanup itself.
/// </remarks>
public interface ISeriesRenderOverride
{
    /// <summary>
    /// Renders <paramref name="series"/> for the current measure pass. Return
    /// <see langword="true"/> if the override took over rendering; return
    /// <see langword="false"/> to fall back to the series' own
    /// <see cref="IChartElement.Invalidate(Chart)"/> (e.g. when a density gate
    /// decides the series is small enough to draw normally).
    /// </summary>
    bool TryRender(ISeries series, Chart chart);

    /// <summary>
    /// Optionally supplies the series' bounds without the per-point fetch — e.g. a
    /// level-of-detail backend reads them from an aggregation pyramid in O(budget)
    /// instead of walking every point. Return <see langword="true"/> with
    /// <paramref name="bounds"/> set to take over; <see langword="false"/> to let the
    /// series compute its own <see cref="ICartesianSeries.GetBounds(Chart, ICartesianAxis, ICartesianAxis)"/>.
    /// </summary>
    bool TryGetBounds(
        ISeries series, Chart chart, ICartesianAxis secondaryAxis, ICartesianAxis primaryAxis,
        out SeriesBounds bounds);

    /// <summary>
    /// Optionally hit-tests the series without the per-point fetch — a level-of-detail
    /// backend finds the nearest decimated point in O(budget) instead of walking every
    /// point on each pointer move. Return the hit points to take over; return
    /// <see langword="null"/> to fall back to
    /// <see cref="ISeries.FindHitPoints(Chart, LvcPoint, FindingStrategy, FindPointFor)"/>.
    /// </summary>
    IEnumerable<ChartPoint>? TryFindHitPoints(
        ISeries series, Chart chart, LvcPoint pointerPosition,
        FindingStrategy strategy, FindPointFor findPointFor);

    /// <summary>
    /// Called when <paramref name="series"/> is removed from the chart, so the
    /// override can release any resources it allocated for it.
    /// </summary>
    void OnRemoved(IChartView view, ISeries series);
}
