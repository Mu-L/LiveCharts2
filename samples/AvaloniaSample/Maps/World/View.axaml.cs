using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.Avalonia;

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

        combo.SelectionChanged += (_, _) =>
        {
            if (combo.SelectedItem is ComboBoxItem item && item.Tag is TooltipPosition pos)
                geoMap.TooltipPosition = pos;
        };
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
