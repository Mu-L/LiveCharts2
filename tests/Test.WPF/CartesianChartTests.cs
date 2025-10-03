using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;

namespace Test.WPF;

[TestClass]
public sealed class CartesianChartTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void Loads()
    {
        TestApp.Run(
            () => new WPFSample.General.FirstChart.View(),
            async ctx =>
            {
                // wait for loading and animations to complete
                await Task.Delay(2000, TestContext.CancellationToken);

                var view = (WPFSample.General.FirstChart.View)ctx.SUT;
                var chart = (CartesianChart)view.Content;
                var series = chart.Series.First();

                if (!chart.CoreChart.IsLoaded)
                    ctx.Throw("Chart is not loaded");

                var points = series.Fetch(chart.CoreChart).ToArray();

                var hasValidRects = points.Length > 0 &&
                    points.All(x =>
                    {
                        // this only validates that rectangles have a size and position
                        // but the proper scale and position is tested in other tests

                        var v = x.Context.Visual;
                        return v is not null && v.X > 0 && v.Y > 0;
                    });

                if (!hasValidRects)
                    ctx.Throw("The series does not have valid rectangles.");

                ctx.EndTest();
            });
    }

    [TestMethod]
    public void Rehydrates()
    {
        Grid grid = null!;
        UserControl sample = null!;

        TestApp.Run(
            () =>
            {
                grid = new Grid();
                sample = new WPFSample.Bars.Basic.View();

                return grid;
            },
            async ctx =>
            {
                var loads = 0;
                var unloads = 0;

                sample.Loaded += (_, _) => loads++;
                sample.Unloaded += (_, _) => unloads++;

                _ = grid.Children.Add(sample);
                await Task.Delay(2000, TestContext.CancellationToken);

                grid.Children.Remove(sample);
                await Task.Delay(500, TestContext.CancellationToken);

                _ = grid.Children.Add(sample);
                await Task.Delay(2000, TestContext.CancellationToken);

                grid.Children.Remove(sample);
                await Task.Delay(500, TestContext.CancellationToken);

                if (loads != 2 || unloads != 2)
                    ctx.Throw($"Expected 2 load and 2 unload events, got {loads} load and {unloads} unload events.");

                ctx.EndTest();
            });
    }

    [TestMethod]
    public void Tabbed()
    {
        TabControl tabs = null!;
        CartesianChart chart = null!;
        ColumnSeries<int> series = null!;

        TestApp.Run(
            () =>
            {
                var firstTab = new TabItem
                {
                    Header = "Tab 1",
                    Content = chart = new CartesianChart
                    {
                        Series = [series = new ColumnSeries<int>([1, 5, 1, 5])]
                    }
                };

                var secondTab = new TabItem
                {
                    Header = "Tab 2",
                    Content = new TextBlock { Text = "Hello World" }
                };

                tabs = new TabControl();

                _ = tabs.Items.Add(firstTab);
                _ = tabs.Items.Add(secondTab);

                return tabs;
            },
            async ctx =>
            {
                await Task.Delay(1000, TestContext.CancellationToken);
                tabs.SelectedIndex = 1;
                await Task.Delay(1000, TestContext.CancellationToken);
                tabs.SelectedIndex = 0;
                await Task.Delay(1000, TestContext.CancellationToken);
                tabs.SelectedIndex = 1;
                await Task.Delay(1000, TestContext.CancellationToken);

                ctx.EndTest();
            });
    }
}
