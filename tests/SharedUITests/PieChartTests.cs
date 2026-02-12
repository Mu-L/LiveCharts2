using Factos;
using SharedUITests.Helpers;
using Xunit;

// to run these tests, see the UITests project, specially the program.cs file.
// to enable IDE intellisense for these tests, go to Directory.Build.props and set UITesting to true.

namespace SharedUITests;

public class PieChartTests
{
    public AppController App => AppController.Current;

    [AppTestMethod]
    public async Task ShouldLoad()
    {
        var sut = await App.NavigateTo<Samples.Pies.Basic.View>();
        await sut.Chart.WaitUntilChartRenders();

        Assert.ChartIsLoaded(sut.Chart);
    }

#if !BLAZOR_UI_TESTING
    // this test makes no sense in blazor.

    [AppTestMethod]
    public async Task ShouldUnloadAndReload()
    {
        var sut = new Samples.Pies.AutoUpdate.View();

        await App.NavigateToView(sut);
        await sut.Chart.WaitUntilChartRenders();
        Assert.ChartIsLoaded(sut.Chart);

#if MAUI_UI_TESTING
        // in maui App.NavigateToView(); uses the Shell navigation,
        // but in this test we need to unload and reload the control in the same view

        sut.UnloadChart();
        await Task.Delay(1000);

        sut.ReloadChart();
        await Task.Delay(1000);

        Assert.ChartIsLoaded(sut.Chart);
#else
        await App.PopNavigation();
        await App.NavigateToView(sut);

        // ToDo: improve this method? as a workaround for now we just wait for some time
        // await sut.Chart.WaitUntilChartRenders();
        await Task.Delay(2000);

        Assert.ChartIsLoaded(sut.Chart);
#endif
    }
#endif

#if (WPF_UI_TESTING && NET6_0_OR_GREATER) || MAUI_UI_TESTING || WINUI_UI_TESTING || (UNO_UI_TESTING && HAS_OS_LVC)
    // native platforms where gpu is supported

    [AppTestMethod]
    public async Task ShouldLoadHardwareAcceleratedView()
    {
        LiveChartsCore.LiveCharts.Configure(config => config.HasRenderingSettings(builder => builder.UseGPU = true));

        var sut = await App.NavigateTo<Samples.Pies.Basic.View>();
        await sut.Chart.WaitUntilChartRenders();

        Assert.Contains("GPU", sut.Chart.CoreCanvas.RendererName);
        Assert.ChartIsLoaded(sut.Chart);

        // restore default settings for other tests
        LiveChartsCore.LiveCharts.Configure(config => config.HasRenderingSettings(builder => builder.UseGPU = false));
    }
#endif
}
