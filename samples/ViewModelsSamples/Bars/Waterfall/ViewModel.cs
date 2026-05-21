using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;

namespace ViewModelsSamples.Bars.Waterfall;

// Waterfall charts visualize a running total through positive and negative
// changes. Each bar's rectangle spans [previousTotal, newTotal] — the exact
// shape RangeColumnSeries renders. The first and last bars in a typical
// waterfall are "anchored" totals at zero (Start and End), and the
// intermediate bars carry the increments / decrements.
//
// This sample shows a simple cash-flow walk:
//   Start ($100)
//     + Sales (+$50) → $150
//     + Other income (+$30) → $180
//     - Costs (-$50) → $130
//     - Tax (-$20) → $110
//   End ($110)
//
// Each RangeValue is (previous total, new total). The bar grows from the
// previous step's top to the new step's top, regardless of direction — the
// MeasureBarLayout helper in CoreRangeColumnSeries normalizes the rect via
// Math.Min / Math.Abs.
public class ViewModel
{
    public ISeries[] Series { get; set; } =
    [
        new RangeColumnSeries<RangeValue>
        {
            Name = "Cash flow",
            Values =
            [
                new(0,  100),    // Start balance
                new(100, 150),   // Sales (+50)
                new(150, 180),   // Other income (+30)
                new(180, 130),   // Costs (-50)
                new(130, 110),   // Tax (-20)
                new(0,  110),    // End balance
            ],
            YToolTipLabelFormatter = point =>
            {
                var from = point.Coordinate.TertiaryValue;
                var to = point.Coordinate.PrimaryValue;
                var delta = to - from;
                var sign = delta >= 0 ? "+" : "−";
                return $"{from:N0} → {to:N0} ({sign}{System.Math.Abs(delta):N0})";
            },
        },
    ];

    public Axis[] XAxes { get; set; } =
    [
        new Axis
        {
            Labels = ["Start", "Sales", "Other income", "Costs", "Tax", "End"],
        },
    ];

    public Axis[] YAxes { get; set; } =
    [
        new Axis
        {
            Name = "Balance",
            MinLimit = 0,
        },
    ];
}
