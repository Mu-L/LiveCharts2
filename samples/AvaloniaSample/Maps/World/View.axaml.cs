using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AvaloniaSample.Maps.World;

public partial class View : UserControl
{
    public View()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        var geoMap = this.Find<GeoMap>("geoMap")!;
        var combo = this.Find<ComboBox>("TooltipPositionCombo")!;
        var resetBtn = this.Find<Button>("ResetZoomBtn")!;

        combo.SelectionChanged += (_, _) =>
        {
            if (combo.SelectedItem is ComboBoxItem item && item.Tag is TooltipPosition pos)
                geoMap.TooltipPosition = pos;
        };

        resetBtn.Click += (_, _) => geoMap.CoreChart.ResetViewport();

        var clearBtn = this.Find<Button>("ClearSeriesBtn")!;
        clearBtn.Click += (_, _) => geoMap.Series = [];

        var borderColorCombo = this.Find<ComboBox>("BorderColorCombo")!;
        borderColorCombo.SelectionChanged += (_, _) =>
        {
            if (borderColorCombo.SelectedItem is ComboBoxItem item && item.Tag is string color)
            {
                geoMap.Stroke = color switch
                {
                    "White" => new SolidColorPaint(SKColors.White) { StrokeThickness = (float)(geoMap.Stroke?.StrokeThickness ?? 1) },
                    "Black" => new SolidColorPaint(SKColors.Black) { StrokeThickness = (float)(geoMap.Stroke?.StrokeThickness ?? 1) },
                    "Gray" => new SolidColorPaint(SKColors.Gray) { StrokeThickness = (float)(geoMap.Stroke?.StrokeThickness ?? 1) },
                    "Red" => new SolidColorPaint(SKColors.Red) { StrokeThickness = (float)(geoMap.Stroke?.StrokeThickness ?? 1) },
                    "None" => null,
                    _ => geoMap.Stroke
                };
            }
        };

        var clickedText = this.Find<TextBlock>("ClickedLandText")!;
        geoMap.CoreChart.LandClicked += args =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                clickedText.Text = $"Clicked: {args.Land.Name} ({args.Land.ShortName}) = {args.Value}");
        };
    }

    private void OnBorderWidthChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        var geoMap = this.Find<GeoMap>("geoMap");
        if (geoMap?.Stroke is not null && e.NewValue.HasValue)
            geoMap.Stroke.StrokeThickness = (float)e.NewValue.Value;
    }

    private void OnTextSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        var geoMap = this.Find<GeoMap>("geoMap");
        if (geoMap is not null && e.NewValue.HasValue)
            geoMap.TooltipTextSize = (double)e.NewValue.Value;
    }

#if UI_TESTING
    public GeoMap Chart => this.Find<GeoMap>("geoMap")!;
#endif
}
