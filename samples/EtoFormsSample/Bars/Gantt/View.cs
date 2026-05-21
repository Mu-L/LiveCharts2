using Eto.Forms;
using LiveChartsCore.SkiaSharpView.Eto;

namespace EtoFormsSample.Bars.Gantt;

public class View : Panel
{
    private readonly CartesianChart cartesianChart;

    public View()
    {
        var vm = new ViewModelsSamples.Bars.Gantt.ViewModel();

        cartesianChart = new CartesianChart
        {
            Series = vm.Series,
            XAxes = vm.XAxes,
            YAxes = vm.YAxes,
        };

        Content = cartesianChart;
    }
}
