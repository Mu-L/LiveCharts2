using Factos;
using SharedUITests.Helpers;
using Xunit;

// to run these tests, see the UITests project, specially the program.cs file.
// to enable IDE intellisense for these tests, go to Directory.Build.props and set UITesting to true.

namespace SharedUITests;

public class CartesianChartTests
{
    public AppController App => AppController.Current;

    [AppTestMethod]
    public async Task ShouldLoad()
    {
        var sut = await App.NavigateTo<Samples.General.FirstChart.View>();
        await sut.Chart.WaitUntilChartRenders();

        Assert.ChartIsLoaded(sut.Chart);
    }

#if XAML_UI_TESTING
    [AppTestMethod]
    public async Task ShouldLoadTemplatedChart()
    {
        var sut = await App.NavigateTo<Samples.VisualTest.DataTemplate.View>();

        // to make it simple, wait for some time for the template to load
        await Task.Delay(2000);

        // now lets find the templated charts
        foreach (var chart in sut.FindCharts())
        {
            await chart.WaitUntilChartRenders();
            Assert.ChartIsLoaded(chart);
        }
    }
#endif

#if !BLAZOR_UI_TESTING
    // this test makes no sense in blazor.

    [AppTestMethod]
    public async Task ShouldUnloadAndReload()
    {
        var sut = new Samples.Bars.AutoUpdate.View();

        await App.NavigateToView(sut);
        await sut.Chart.WaitUntilChartRenders();
        Assert.ChartIsLoaded(sut.Chart);

        await App.PopNavigation();

        await App.NavigateToView(sut);
        await sut.Chart.WaitUntilChartRenders();
        Assert.ChartIsLoaded(sut.Chart);
    }
#endif
}
