﻿// The MIT License(MIT)
//
// Copyright(c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using LiveChartsCore.Generators;
using LiveChartsCore.Kernel.Sketches;

#if AVALONIA_LVC
using BaseControl = Avalonia.Controls.Control;
namespace LiveChartsCore.SkiaSharpView.Avalonia;
#elif MAUI_LVC
using BaseControl = LiveChartsCore.SkiaSharpView.Maui.Handlers.EmptyContentView;
namespace LiveChartsCore.SkiaSharpView.Maui;
#elif WINUI_LVC
using BaseControl = Microsoft.UI.Xaml.FrameworkElement;
namespace LiveChartsCore.SkiaSharpView.WinUI;
#elif WPF_LVC
using BaseControl = System.Windows.FrameworkElement;
namespace LiveChartsCore.SkiaSharpView.WPF;
#endif

/// <inheritdoc cref="PolarAxis"/>
#if WINUI_LVC
[XamlClass(typeof(PolarAxis), PropertyChangeMap = "MinLimit{=}MinLimitMap{,}MaxLimit{=}MaxLimitMap")]
#elif AVALONIA_LVC
[XamlClass(typeof(PolarAxis), GenerateOnChange = false)]
#else
[XamlClass(typeof(PolarAxis))]
#endif
public partial class XamlPolarAxis : BaseControl, IPolarAxis
{
    string? IPlane.Name { get => _baseType.Name; set => _baseType.Name = value; }

#if WINUI_LVC
    // WINUI does not support nullable value types in xaml, we map null to NaN

    double? IPlane.MinLimit { get => _baseType.MinLimit; set => _baseType.MinLimit = value; }
    double? IPlane.MaxLimit { get => _baseType.MaxLimit; set => _baseType.MaxLimit = value; }

    private void MinLimitMap(object value)
    {
        var doubleValue = (double)value;
        _baseType.MinLimit = double.IsNaN(doubleValue) ? null : doubleValue;
    }

    private void MaxLimitMap(object value)
    {
        var doubleValue = (double)value;
        _baseType.MaxLimit = double.IsNaN(doubleValue) ? null : doubleValue;
    }
#endif
}
