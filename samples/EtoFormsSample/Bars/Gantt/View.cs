using Eto.Forms;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Eto;

namespace EtoFormsSample.Bars.Gantt;

public class View : Panel
{
    private readonly CartesianChart cartesianChart;

    public View()
    {
        // Each RangeValue is one task's [start day, end day]; the Y axis
        // Labels array supplies per-row task names, lined up by index.
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

        cartesianChart = new CartesianChart
        {
            Series = series,
            XAxes = [new Axis { Name = "Day", MinLimit = 0 }],
            YAxes = [new Axis { Labels = taskNames }],
        };

        Content = cartesianChart;
    }
}
