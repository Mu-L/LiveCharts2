using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaloniaSample.VisualTest.Issue1986Repro;

public partial class View : UserControl
{
    private readonly ViewModel _vm;

    public View()
    {
        _vm = new ViewModel();
        DataContext = _vm;
        InitializeComponent();
        _ = _vm.LoadDataAsync(1000);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
