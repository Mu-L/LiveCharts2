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

using Factos;
using LiveChartsCore.Kernel.Sketches;
using Xunit;

namespace SharedUITests.Helpers;

public static class UIHelpersExtensions
{
    extension(AppController controller)
    {
        public async Task<T> NavigateTo<T>()
            where T : class, new()
        {
#if BLAZOR_UI_TESTING
            var blazorController = (Factos.Blazor.BlazorAppController)controller;
            return await blazorController.NavigateToView<T>();
#else
            var view = new T();
            await controller.NavigateToView(view);
            return view;
#endif
        }

    }

    extension(IChartView chartView)
    {
        public Task WaitUntilChartRenders()
        {
            var tcs = new TaskCompletionSource<object>();

            if (chartView.CoreCanvas.IsValid)
            {
                tcs.SetResult(new());
                return tcs.Task;
            }

            // force an update, then wait for the update to start in the ui thread
            chartView.CoreChart.Update(new LiveChartsCore.Kernel.ChartUpdateParams
            {
                IsAutomaticUpdate = false,
                Throttling = false
            });

            void Handler(IChartView chart)
            {
                chartView.UpdateStarted -= Handler;
                tcs.SetResult(new());
            }

            chartView.UpdateStarted += Handler;

            return tcs.Task;
        }
    }

    extension(Assert)
    {
        public static void ChartIsLoaded(IChartView chartView)
        {
            Assert.True(chartView.CoreChart.IsLoaded);

            var fetchedPoints = chartView.Series
                .SelectMany(s => s.Fetch(chartView.CoreChart))
                .ToArray();

            Assert.NotEmpty(fetchedPoints);

            Assert.All(fetchedPoints, point =>
            {
                // this only validates that points have a size and position
                // but the proper scale and position is tested in LiveChartsCore.UnitTesting

                var v = point.Context.Visual;
                Assert.NotNull(v);
                Assert.True(v.X > 0);
                Assert.True(v.Y > 0);
            });
        }
    }
}
