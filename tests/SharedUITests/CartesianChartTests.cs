using System.Threading.Tasks;
using Factos;
using SharedUITests.Helpers;
using Xunit;

#if AVALONIA_UI_TESTING
using ShouldLoadCartesianSUT = AvaloniaSample.General.FirstChart.View;
using ShouldUnloadAndLoadAgainCartesianSUT = AvaloniaSample.Bars.AutoUpdate.View;
#endif

#if BLAZOR_UI_TESTING
using ShouldLoadCartesianSUT = BlazorSample.Pages.General.FirstChart;
#endif

#if ETO_UI_TESTING
using ShouldLoadCartesianSUT = EtoFormsSample.General.FirstChart.View;
using ShouldUnloadAndLoadAgainCartesianSUT = EtoFormsSample.Bars.AutoUpdate.View;
#endif

#if MAUI_UI_TESTING
using ShouldLoadCartesianSUT = MauiSample.General.FirstChart.View;
using ShouldUnloadAndLoadAgainCartesianSUT = MauiSample.Bars.AutoUpdate.View;
#endif

#if UNO_UI_TESTING || WINUI_UI_TESTING
using ShouldLoadCartesianSUT = WinUISample.General.FirstChart.View;
using ShouldUnloadAndLoadAgainCartesianSUT = WinUISample.Bars.AutoUpdate.View;
#endif

#if WINFORMS_UI_TESTING
using ShouldLoadCartesianSUT = WinFormsSample.General.FirstChart.View;
using ShouldUnloadAndLoadAgainCartesianSUT = WinFormsSample.Bars.AutoUpdate.View;
#endif

#if WPF_UI_TESTING
using ShouldLoadCartesianSUT = WPFSample.General.FirstChart.View;
using ShouldUnloadAndLoadAgainCartesianSUT = WPFSample.Bars.AutoUpdate.View;
#endif

namespace SharedUITests;

public class CartesianChartTests
{
    public AppController App => AppController.Current;

    [AppTestMethod]
    public async Task ShouldLoad()
    {
        var sut = await App.NavigateTo<ShouldLoadCartesianSUT>();
        await sut.Chart.WaitUntilChartRenders();

        Assert.ChartIsLoaded(sut.Chart);
    }

#if !BLAZOR_UI_TESTING
    // this test makes no sense in blazor.

    [AppTestMethod]
    public async Task ShouldUnloadAndReload()
    {
        var sut = new ShouldUnloadAndLoadAgainCartesianSUT();

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
