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

using System;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView.Maui.Handlers;
using Microsoft.Maui.Hosting;

namespace LiveChartsCore.SkiaSharpView.Maui;

/// <summary>
/// LiveCharts extensions for the <see cref="MauiAppBuilder"/> class.
/// </summary>
public static class LiveChartsCoreMauiAppBuilderExtensions
{
    internal static bool AreHandlersRegistered { get; private set; }

    /// <summary>
    /// Adds LiveCharts components to the MAUI app.
    /// </summary>
    /// <param name="mauiAppBuilder">The Maui app buylder.</param>
    /// <param name="configuration">An optional action to configure LiveCharts settings.</param>
    /// <returns>The current MAui app builder.</returns>
    public static MauiAppBuilder UseLiveCharts(
        this MauiAppBuilder mauiAppBuilder, Action<LiveChartsSettings>? configuration = null)
    {
        if (!AreHandlersRegistered)
        {
            _ = mauiAppBuilder
                .ConfigureMauiHandlers(handlers => handlers
                    .AddHandler<ChartView, ChartViewHandler>()
                    .AddHandler<EmptyContentView, EmptyViewHandler>());
        }

        AreHandlersRegistered = true;

        if (configuration is not null)
            LiveCharts.Configure(configuration);

        return mauiAppBuilder;
    }
}
