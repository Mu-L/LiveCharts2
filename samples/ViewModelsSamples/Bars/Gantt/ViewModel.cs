using System;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;

namespace ViewModelsSamples.Bars.Gantt;

// Gantt charts present each task as a horizontal bar spanning [start, end] on
// a date / time axis, with the Y axis listing task names. RangeRowSeries fits
// this exactly: each RangeValue is one task with Low = start.Ticks and
// High = end.Ticks; DateTimeAxis on X converts the ticks back to dates for
// the tick labels, and a custom tooltip formatter prints the date range on
// hover.
public class ViewModel
{
    private static readonly DateTime s_projectStart = new(2026, 6, 1);

    public ISeries[] Series { get; set; } =
    [
        new RangeRowSeries<RangeValue>
        {
            Name = "Project",
            Values =
            [
                MakeTask(0,  5),   // Design
                MakeTask(3,  8),   // Backend API
                MakeTask(5, 12),   // Frontend
                MakeTask(7, 14),   // Integration
                MakeTask(10, 18),  // Testing
                MakeTask(14, 20),  // Deploy
            ],
            // The bar's Coordinate carries TertiaryValue = start.Ticks and
            // PrimaryValue = end.Ticks (see RangeValue.OnCoordinateChanged).
            // Convert each back to a DateTime for a human-friendly tooltip.
            XToolTipLabelFormatter = point =>
            {
                var start = new DateTime((long)point.Coordinate.TertiaryValue);
                var end = new DateTime((long)point.Coordinate.PrimaryValue);
                return $"from {start:MMM dd} to {end:MMM dd}";
            },
        },
    ];

    public Axis[] XAxes { get; set; } =
    [
        // 2-day stepping matches the project's ~20-day total — increase the
        // unit for longer projects so the X axis isn't crowded with labels.
        new DateTimeAxis(TimeSpan.FromDays(2), date => date.ToString("MMM dd"))
        {
            Name = "Date",
        },
    ];

    public Axis[] YAxes { get; set; } =
    [
        new Axis
        {
            Labels = [
                "Design",
                "Backend API",
                "Frontend",
                "Integration",
                "Testing",
                "Deploy",
            ],
        },
    ];

    private static RangeValue MakeTask(int startDayOffset, int endDayOffset) =>
        new(s_projectStart.AddDays(startDayOffset).Ticks,
            s_projectStart.AddDays(endDayOffset).Ticks);
}
