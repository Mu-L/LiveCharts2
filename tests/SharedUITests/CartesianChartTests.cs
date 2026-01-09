using Factos;
using SharedUITests.Helpers;
using Xunit;

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
