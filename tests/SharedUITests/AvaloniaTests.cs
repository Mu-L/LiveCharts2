#if AVALONIA_UI_TESTING

using Factos;
using SharedUITests.Helpers;
using Xunit;

namespace SharedUITests;

public class AvaloniaTests
{
    public AppController App => AppController.Current;

    // based on:
    // https://github.com/Live-Charts/LiveCharts2/issues/1986
    // ensure charts load when avalonia virtualization is on.
    [AppTestMethod]
    public async Task ShouldLoadTemplatedChart()
    {
        var sut = await App.NavigateTo<Samples.VisualTest.VirtualizationTest.View>();

        // open the second tab, scroll to end and ensure the chart is loaded.
        await Task.Delay(1000);
        sut.OpenTab2();
        await Task.Delay(1000);
        sut.ScrollToChart();
        await Task.Delay(1000);
        Assert.ChartIsLoaded(sut.Chart2);

        // now open the first tab, scroll to end and ensure the chart is loaded.
        await Task.Delay(1000);
        sut.OpenTab1();
        await Task.Delay(1000);
        sut.ScrollToChart();
        await Task.Delay(1000);
        Assert.ChartIsLoaded(sut.Chart1);
    }
}

#endif
