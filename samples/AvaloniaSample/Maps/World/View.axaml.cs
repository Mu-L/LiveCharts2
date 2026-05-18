using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LiveChartsCore.Geo;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
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
        HeatLand[]? savedLands = null;
        clearBtn.Click += (_, _) =>
        {
            var series = geoMap.Series?.Cast<HeatLandSeries>().FirstOrDefault();
            if (series is null) return;

            if (savedLands is null)
            {
                savedLands = series.Lands?.Cast<HeatLand>().ToArray();
                series.Lands = [];
                clearBtn.Content = "Restore Series";
            }
            else
            {
                series.Lands = savedLands;
                savedLands = null;
                clearBtn.Content = "Clear Series";
            }
        };

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
        geoMap.DataPointerDown += (_, points) =>
        {
            var land = points.FirstOrDefault()?.Context.DataSource as LandDefinition;
            if (land is null) return;

            var value = 0d;
            foreach (var series in geoMap.Series ?? [])
                if (series.TryGetValue(land.ShortName, out value)) break;

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                clickedText.Text = $"Clicked: {land.Name} ({land.ShortName}) = {value}");
        };
    }

    private void OnBorderWidthChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        var geoMap = this.Find<GeoMap>("geoMap");
        if (geoMap?.Stroke is not null && e.NewValue.HasValue)
        {
            geoMap.Stroke.StrokeThickness = (float)e.NewValue.Value;
            geoMap.CoreChart.Update();
        }
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
