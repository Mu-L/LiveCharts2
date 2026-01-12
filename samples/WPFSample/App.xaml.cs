using System;
using System.Windows;
using ViewModelsSamples;
using LiveChartsCore;
using Factos.WPF; // mark

namespace WPFSample;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // LiveCharts configuration section: // mark
        LiveCharts.Configure(c => c // mark
            .AddLiveChartsAppSettings()); // mark

#if UI_TESTING
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Factos.SGTests).TypeHandle);
        this.UseFactosApp();
#endif
    }
}
