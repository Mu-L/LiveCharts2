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

using System.Collections.ObjectModel;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsGeneratedCode;

namespace LiveChartsCore.SkiaSharpView.SKCharts;

/// <inheritdoc cref="ISankeyChartView"/>
public class SKSankeyChart : SourceGenSKChart, ISankeyChartView
{
    /// <summary>Initializes a new instance of the <see cref="SKSankeyChart"/> class.</summary>
    public SKSankeyChart() : base(null) { }

    /// <summary>Initializes a new instance from an existing chart view.</summary>
    public SKSankeyChart(IChartView chartView) : base(chartView) { }

    SankeyChartEngine ISankeyChartView.Core => (SankeyChartEngine)CoreChart;

    /// <inheritdoc cref="SourceGenSKChart.CreateCoreChart"/>
    protected override Chart CreateCoreChart() => new SankeyChartEngine(this, CoreCanvas);

    /// <inheritdoc cref="SourceGenSKChart.InitializeObservedProperties"/>
    protected override void InitializeObservedProperties()
    {
        SyncContext = new object();
        Series = new ObservableCollection<ISeries>();
        VisualElements = new ObservableCollection<IChartElement>();
    }
}
