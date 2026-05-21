using System.Windows.Forms;
using LiveChartsCore.SkiaSharpView.WinForms;

namespace WinFormsSample.Bars.Gantt;

public partial class View : UserControl
{
    public View()
    {
        InitializeComponent();
        Size = new System.Drawing.Size(50, 50);

        var vm = new ViewModelsSamples.Bars.Gantt.ViewModel();

        var cartesianChart = new CartesianChart
        {
            Series = vm.Series,
            XAxes = vm.XAxes,
            YAxes = vm.YAxes,
            Location = new System.Drawing.Point(0, 0),
            Size = new System.Drawing.Size(50, 50),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom
        };

        Controls.Add(cartesianChart);
    }
}
