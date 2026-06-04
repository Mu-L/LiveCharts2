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

namespace LiveChartsCore.Kernel.Providers;

/// <summary>
/// Optional marker an <see cref="ISeriesRenderOverride"/> implements when it hosts its visuals on
/// the overridden series' OWN paint tasks (reusing them, so the series' gradient/effects apply) and
/// manages clearing the series' frozen per-point visuals itself. The chart then SKIPS its generic
/// engage-cleanup (which removes the series' paints) for such a series, so it does not pull the
/// reused paint out from under the override. An override that does NOT implement this draws on its
/// own paints and the chart drops the overridden series' visuals on engage.
/// </summary>
/// <remarks>
/// This is a marker interface rather than a member on <see cref="ISeriesRenderOverride"/> because
/// the library targets net462 / netstandard2.0, which do not support default interface members;
/// a marker keeps the capability optional (and additive) across every target framework.
/// </remarks>
public interface IReusesSeriesPaints
{
}
