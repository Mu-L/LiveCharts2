using LiveChartsCore;
using Microsoft.UI.Xaml;
using ViewModelsSamples;

namespace UnoPlatformSample;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Recommended place to configure LiveCharts.
        LiveCharts.Configure(c => c.AddLiveChartsAppSettings());

        MainWindow = new Window();
        if (MainWindow.Content is not MainPage)
            MainWindow.Content = new MainPage();

        MainWindow.Activate();
    }
}
