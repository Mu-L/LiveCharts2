using LiveChartsCore.Defaults;

namespace ViewModelsSamples.Bars.Gantt;

// Gantt charts present each task as a horizontal bar spanning [start, end] on
// a time / day axis, with the Y axis listing task names. RangeRowSeries fits
// this exactly: each RangeValue is one task; the Y category labels live on
// the Y axis labels. Multiple tasks share a single series.
public class ViewModel
{
    public RangeValue[] Tasks { get; set; } =
    [
        new(0,  5),   // Design
        new(3,  8),   // Backend API
        new(5, 12),   // Frontend
        new(7, 14),   // Integration
        new(10, 18),  // Testing
        new(14, 20),  // Deploy
    ];

    public string[] TaskNames { get; set; } =
    [
        "Design",
        "Backend API",
        "Frontend",
        "Integration",
        "Testing",
        "Deploy",
    ];
}
