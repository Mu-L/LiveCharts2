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

#pragma warning disable IDE1006 // Naming Styles

using LiveChartsCore.Generators;
using LiveChartsCore.SkiaSharpView.VisualElements;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.Kernel;
using LiveChartsCore.VisualElements;
using System;

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

[XamlClass(typeof(DrawnLabelVisual), Map = typeof(LabelGeometry), MapPath = "DrawnLabel")]
public partial class XamlDrawnLabelVisual : BaseControl, IChartElement, IInternalInteractable
{
    private static readonly LabelGeometry _defaultDrawnLabel = new()
    {
        HorizontalAlign = LiveChartsCore.Drawing.Align.Start,
        VerticalAlign = LiveChartsCore.Drawing.Align.Start
    };
    private LabelGeometry DrawnLabel => (LabelGeometry?)_baseType.DrawnElement
        ?? throw new Exception("Drawn element not found");
}

#if WINUI_LVC
[XamlClass(typeof(RectangularSection), PropertyChangeMap = "Xi{=}XiMap{,}Xj{=}XjMap")]
#else
[XamlClass(typeof(RectangularSection))]
#endif
public partial class XamlRectangularSection : BaseControl, IChartElement
{
#if WINUI_LVC
    // WINUI does not support nullable value types in xaml, we map null to NaN

    private void XiMap(object value)
    {
        var doubleValue = (double)value;
        _baseType.Xi = double.IsNaN(doubleValue) ? null : doubleValue;
    }

    private void XjMap(object value)
    {
        var doubleValue = (double)value;
        _baseType.Xj = double.IsNaN(doubleValue) ? null : doubleValue;
    }
#endif
}

[XamlClass(typeof(NeedleVisual))]
public partial class XamlNeedle : BaseControl, IChartElement, IInternalInteractable { }

[XamlClass(typeof(AngularTicksVisual))]
public partial class XamlAngularTicks : BaseControl, IChartElement, IInternalInteractable { }

/// <inheritdoc cref="BaseSharedAxesPair"/>
public class SharedAxesPair : BaseSharedAxesPair { }

/// <inheritdoc cref="BaseChartPointState"/>
#if MAUI_LVC
[Microsoft.Maui.Controls.ContentProperty(nameof(Setters))]
#endif
public class ChartPointState : BaseChartPointState { }

/// <inheritdoc cref="BaseSet"/>
public class Set : BaseSet { }

/// <summary>
/// A series collection for WPF XAML parser.
/// </summary>
public class SeriesCollection : System.Collections.ObjectModel.ObservableCollection<ISeries> { }

/// <summary>
/// An axes collection for WPF XAML parser.
/// </summary>
public class AxesCollection : System.Collections.ObjectModel.ObservableCollection<Kernel.Sketches.ICartesianAxis> { }

/// <summary>
/// An axes collection for WPF XAML parser.
/// </summary>
public class PolarAxesCollection : System.Collections.ObjectModel.ObservableCollection<Kernel.Sketches.IPolarAxis> { }

/// <summary>
/// An axes collection for WPF XAML parser.
/// </summary>
public class SectionsCollection : System.Collections.ObjectModel.ObservableCollection<IChartElement> { }

/// <summary>
/// An axes collection for WPF XAML parser.
/// </summary>
public class VisualsCollection : System.Collections.ObjectModel.ObservableCollection<IChartElement> { }
