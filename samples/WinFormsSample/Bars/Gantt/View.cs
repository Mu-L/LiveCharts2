using System.Windows.Forms;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinForms;

namespace WinFormsSample.Bars.Gantt;

public partial class View : UserControl
{
    public View()
    {
        InitializeComponent();
        Size = new System.Drawing.Size(50, 50);

        // Each RangeValue represents one task spanning [start day, end day].
        // The Y axis Labels array supplies the per-row task names, lined up
        // with each bar by index.
        var tasks = new[]
        {
            new RangeValue(0,  5),
            new RangeValue(3,  8),
            new RangeValue(5, 12),
            new RangeValue(7, 14),
            new RangeValue(10, 18),
            new RangeValue(14, 20),
        };

        var taskNames = new[]
        {
            "Design",
            "Backend API",
            "Frontend",
            "Integration",
            "Testing",
            "Deploy",
        };

        var series = new ISeries[]
        {
            new RangeRowSeries<RangeValue>
            {
                Name = "Project",
                Values = tasks,
            },
        };

        var cartesianChart = new CartesianChart
        {
            Series = series,
            XAxes = [new Axis { Name = "Day", MinLimit = 0 }],
            YAxes = [new Axis { Labels = taskNames }],
            Location = new System.Drawing.Point(0, 0),
            Size = new System.Drawing.Size(50, 50),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom
        };

        Controls.Add(cartesianChart);
    }
}
