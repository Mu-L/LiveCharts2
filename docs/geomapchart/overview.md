<!--
To get help on editing this file, see https://github.com/beto-rodriguez/LiveCharts2/blob/master/docs/readme.md
-->

# The GeoMap Chart

The `GeoMap` control is useful to create geographical maps, it uses files in [geojson](https://en.wikipedia.org/wiki/GeoJSON) format to render
vectorized maps.

![image](https://raw.githubusercontent.com/beto-rodriguez/LiveCharts2/master/docs/_assets/geomaphs.png)

{{~ if xaml ~}}
<pre><code>using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;

namespace ViewModelsSamples.Maps.World
{
    public class ViewModel
    {
        public HeatLandSeries[] Series { get; set; }
            = new HeatLandSeries[]
            {
                new HeatLandSeries
                {
                    // every country has a unique identifier
                    // check the "shortName" property in the following
                    // json file to assign a value to a country in the heat map
                    // https://github.com/beto-rodriguez/LiveCharts2/blob/master/docs/_assets/word-map-index.json
                    Lands = new HeatLand[]
                    {
                        new HeatLand { Name = "bra", Value = 13 },
                        new HeatLand { Name = "mex", Value = 10 },
                        new HeatLand { Name = "usa", Value = 15 },
                        new HeatLand { Name = "deu", Value = 13 },
                        new HeatLand { Name = "fra", Value = 8 },
                        new HeatLand { Name = "kor", Value = 10 },
                        new HeatLand { Name = "zaf", Value = 12 },
                        new HeatLand { Name = "are", Value = 13 }
                    }
                }
            };
    }
}</code></pre>

<pre><code>&lt;lvc:GeoMap Series="{Binding Series}">&lt;/lvc:GeoMap></code></pre>
{{~ end ~}}

{{~ if blazor ~}}
<pre><code>@using LiveChartsCore.SkiaSharpView;
@using LiveChartsCore.SkiaSharpView.Drawing.Geometries;

&lt;GeoMap Series="series">&lt;/GeoMap>

@code {
    private HeatLandSeries[] series = new HeatLandSeries[]
    {
        new HeatLandSeries
        {
            // every country has a unique identifier
            // check the "shortName" property in the following
            // json file to assign a value to a country in the heat map
            // https://github.com/beto-rodriguez/LiveCharts2/blob/master/docs/_assets/word-map-index.json
            Lands = new HeatLand[]
            {
                new HeatLand { Name = "bra", Value = 13 },
                new HeatLand { Name = "mex", Value = 10 },
                new HeatLand { Name = "usa", Value = 15 },
                new HeatLand { Name = "deu", Value = 13 },
                new HeatLand { Name = "fra", Value = 8 },
                new HeatLand { Name = "kor", Value = 10 },
                new HeatLand { Name = "zaf", Value = 12 },
                new HeatLand { Name = "are", Value = 13 }
            }
        }
    };
}</code></pre>
{{~ end ~}}

{{~ if winforms ~}}
<pre><code>geoMap1.Series = new HeatLandSeries[]
{
    new HeatLandSeries
    {
        // every country has a unique identifier
        // check the "shortName" property in the following
        // json file to assign a value to a country in the heat map
        // https://github.com/beto-rodriguez/LiveCharts2/blob/master/docs/_assets/word-map-index.json
        Lands = new HeatLand[]
        {
            new HeatLand { Name = "bra", Value = 13 },
            new HeatLand { Name = "mex", Value = 10 },
            new HeatLand { Name = "usa", Value = 15 },
            new HeatLand { Name = "deu", Value = 13 },
            new HeatLand { Name = "fra", Value = 8 },
            new HeatLand { Name = "kor", Value = 10 },
            new HeatLand { Name = "zaf", Value = 12 },
            new HeatLand { Name = "are", Value = 13 }
        }
    }
};</code></pre>
{{~ end ~}}

## Series

There are ~multiple~ series available in the library, you can add one or mix them all in the same chart, every series has unique properties,
any image below is a link to an article explaining more about them.

<a href="{{ website_url }}/docs/{{ platform }}/{{ version }}/GeoMap.Heat%20land%20series">
<div class="series-miniature">
<img src="https://raw.githubusercontent.com/beto-rodriguez/LiveCharts2/master/docs/samples/polarLines/basic/geomaphs.png" alt="series"/>
<div class="text-center"><b>Heat Land series</b></div>
</div>
</a>

## Stroke property

Determines the default stroke of every land, if the stroke property is not set, then LiveCharts will create it based on the series position in your series collection
and the current theme.

{{~ if xaml ~}}
<pre><code>using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;

namespace ViewModelsSamples.Maps.World
{
    public class ViewModel
    {
        public HeatLandSeries[] Series { get; set; }
            = new HeatLandSeries[]
            {
                new HeatLandSeries { Lands = new HeatLand[] { ... } }
            };

        public SolidColorPaint Stroke { get; set; } 
            = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 }; // mark
    }
}</code></pre>

<pre><code>&lt;lvc:GeoMap
    Series="{Binding Series}"
    Stroke="{Binding Stroke}">&lt;!-- mark -->
&lt;/lvc:GeoMap></code></pre>
{{~ end ~}}

{{~ if blazor ~}}
<pre><code>@using LiveChartsCore.SkiaSharpView;
@using LiveChartsCore.SkiaSharpView.Drawing.Geometries;

&lt;GeoMap
    Series="series"
    Stroke="stroke">&lt;!-- mark -->
&lt;/GeoMap>

@code {
    private HeatLandSeries[] series = new HeatLandSeries[]
    {
        new HeatLandSeries { Lands = new HeatLand[] { ... } }
    };

    private SolidColorPaint stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 }; // mark
}</code></pre>
{{~ end ~}}

{{~ if winforms ~}}
<pre><code>geoMap1.Series = new HeatLandSeries[]
{
    new HeatLandSeries
    {
        Lands = new HeatLand[] { ... }
    }
};
geoMap1.Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 }; // mark</code></pre>
{{~ end ~}}

![image](https://raw.githubusercontent.com/beto-rodriguez/LiveCharts2/master/docs/_assets/geomap-stroke.png)

:::info
Paints can create gradients, dashed lines and more, if you need help using the `Paint` instances take 
a look at the [Paints article]({{ website_url }}/docs/{{ platform }}/{{ version }}/Overview.Paints).
:::

## Fill property

Determines the default fill of every land, if the stroke property is not set, then LiveCharts will create it based on the series position in your series collection
and the current theme.

{{~ if xaml ~}}
<pre><code>using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;

namespace ViewModelsSamples.Maps.World
{
    public class ViewModel
    {
        public HeatLandSeries[] Series { get; set; }
            = new HeatLandSeries[]
            {
                new HeatLandSeries { Lands = new HeatLand[] { ... } }
            };

        public SolidColorPaint Fill { get; set; } = new SolidColorPaint(SKColors.LightPink); // mark
    }
}</code></pre>

<pre><code>&lt;lvc:GeoMap
    Series="{Binding Series}"
    Fill="{Binding Fill}">&lt;!-- mark -->
&lt;/lvc:GeoMap></code></pre>
{{~ end ~}}

{{~ if blazor ~}}
<pre><code>@using LiveChartsCore.SkiaSharpView;
@using LiveChartsCore.SkiaSharpView.Drawing.Geometries;

&lt;GeoMap
    Series="series"
    Fill="fill">&lt;!-- mark -->
&lt;/GeoMap>

@code {
    private HeatLandSeries[] series = new HeatLandSeries[]
    {
        new HeatLandSeries { Lands = new HeatLand[] { ... } }
    };

    private SolidColorPaint fill = new SolidColorPaint(SKColors.LightPink); // mark
}</code></pre>
{{~ end ~}}

{{~ if winforms ~}}
<pre><code>geoMap1.Series = new HeatLandSeries[]
{
    new HeatLandSeries
    {
        Lands = new HeatLand[] { ... }
    }
};
geoMap1.Fill = new SolidColorPaint(SKColors.LightPink); // mark</code></pre>
{{~ end ~}}

![image](https://raw.githubusercontent.com/beto-rodriguez/LiveCharts2/master/docs/_assets/geomap-fill.png)

:::info
Paints can create gradients, dashed lines and more, if you need help using the `Paint` instances take 
a look at the [Paints article]({{ website_url }}/docs/{{ platform }}/{{ version }}/Overview.Paints).
:::

## MapProjection property

Defines the [projection](https://en.wikipedia.org/wiki/Map_projection) of the map coordinates in the control coordinates,
currently it only support the `Default` (none) and `Mercator` projections.

{{~ if xaml ~}}
<pre><code>&lt;lvc:GeoMap
    Series="{Binding Series}"
    MapProjection="Mercator">&lt;!-- mark -->
&lt;/lvc:GeoMap></code></pre>
{{~ end ~}}

{{~ if blazor ~}}
<pre><code>&lt;GeoMap
    Series="series"
    MapProjection="LiveChartsCore.Geo.MapProjection.Mercator">&lt;!-- mark --> 
&lt;/GeoMap></code></pre>
{{~ end ~}}

{{~ if winforms ~}}
<pre><code>geoMap1.MapProjection = LiveChartsCore.Geo.MapProjection.Mercator;</code></pre>
{{~ end ~}}

![image](https://raw.githubusercontent.com/beto-rodriguez/LiveCharts2/master/docs/_assets/geomap-mercator.png)

## InteractionMode property

Controls which user interactions the map responds to. Defaults to
`MapInteractionMode.Zoom` — mouse wheel zooms, click-drag does **not** pan.
Set it to `Both` to enable click-drag panning, or `None` to make the map
static.

| Value  | Wheel zoom | Click-drag pan |
| ------ | ---------- | -------------- |
| `None` | ✗          | ✗              |
| `Pan`  | ✗          | ✓              |
| `Zoom` | ✓          | ✗ *(default)*  |
| `Both` | ✓          | ✓              |

{{~ if xaml ~}}
<pre><code>&lt;lvc:GeoMap
    Series="{Binding Series}"
    InteractionMode="Both">&lt;!-- mark -->
&lt;/lvc:GeoMap></code></pre>
{{~ end ~}}

{{~ if blazor ~}}
<pre><code>&lt;GeoMap
    Series="series"
    InteractionMode="LiveChartsCore.Geo.MapInteractionMode.Both">&lt;!-- mark -->
&lt;/GeoMap></code></pre>
{{~ end ~}}

{{~ if winforms ~}}
<pre><code>geoMap1.InteractionMode = LiveChartsCore.Geo.MapInteractionMode.Both;</code></pre>
{{~ end ~}}

To hide the tooltip, set `TooltipPosition` to `Hidden`:

{{~ if xaml ~}}
<pre><code>&lt;lvc:GeoMap
    Series="{Binding Series}"
    TooltipPosition="Hidden">&lt;!-- mark -->
&lt;/lvc:GeoMap></code></pre>
{{~ end ~}}

{{~ if blazor ~}}
<pre><code>&lt;GeoMap
    Series="series"
    TooltipPosition="LiveChartsCore.Measure.TooltipPosition.Hidden">&lt;!-- mark -->
&lt;/GeoMap></code></pre>
{{~ end ~}}

{{~ if winforms ~}}
<pre><code>geoMap1.TooltipPosition = LiveChartsCore.Measure.TooltipPosition.Hidden;</code></pre>
{{~ end ~}}

## Finding lands on click or hover

The map participates in the same `IChartView` pointer-event surface as the
other charts. `DataPointerDown` fires once per land click, and
`HoveredPointsChanged` fires when the pointer enters, transitions between, or
leaves a land. Each `ChartPoint` carries the `LandDefinition` as its data
source — unwrap it to read the land's name / short name and look up the
per-series values yourself.

<pre><code>using LiveChartsCore.Geo;
using LiveChartsCore.Kernel;

geoMap.DataPointerDown += (sender, points) =>
{
    if (points.FirstOrDefault()?.Context.DataSource is not LandDefinition land) return;

    // Look up each series' value for this land.
    foreach (var series in geoMap.Series ?? [])
        if (series.TryGetValue(land.ShortName, out var value))
            Console.WriteLine($"{series.Name}: {value}");
};</code></pre>

If you only need a synchronous lookup (e.g. on a custom gesture), call
`geoMap.GetPointsAt(new LvcPointD(x, y))` — same `ChartPoint` shape, no event
subscription needed.

## Customizing the tooltip

The default tooltip (`SKDefaultGeoTooltip`) renders the land name followed by
one labeled line per heat series that has a value for it. For most cases the
quickest knob is `TooltipFormatter` — a `Func<GeoTooltipValue, string>` that
takes over the per-value line text:

{{~ if xaml ~}}
<pre><code>// In your ViewModel:
public Func&lt;GeoTooltipValue, string> TooltipFormatter { get; }
    = v => $"{v.Series.Name}: {v.Value:C0}"; // mark</code></pre>

<pre><code>&lt;lvc:GeoMap
    Series="{Binding Series}"
    TooltipFormatter="{Binding TooltipFormatter}">&lt;!-- mark -->
&lt;/lvc:GeoMap></code></pre>
{{~ end ~}}

{{~ if blazor ~}}
<pre><code>&lt;GeoMap
    Series="series"
    TooltipFormatter="@(v => $"{v.Series.Name}: {v.Value:C0}")">&lt;!-- mark -->
&lt;/GeoMap></code></pre>
{{~ end ~}}

{{~ if winforms ~}}
<pre><code>geoMap1.TooltipFormatter = v => $"{v.Series.Name}: {v.Value:C0}"; // mark</code></pre>
{{~ end ~}}

The default format is `"{Series.Name}: {Value:N2}"` (or just `"{Value:N2}"`
when the series has no `Name`). When several series cover the same land, you
get one line per series in the order they appear in `Series`.

For deeper customization (layout, multiple paints, icons, etc.), subclass
`SKDefaultGeoTooltip` or implement `IGeoMapTooltip` from scratch and assign
it to the `Tooltip` property:

<pre><code>public class MyTooltip : SKDefaultGeoTooltip
{
    protected override Layout&lt;SkiaSharpDrawingContext> GetLayout(
        GeoTooltipPoint point, GeoMapChart chart, Theme theme, PopUpPlacement placement)
    {
        // build and return your own layout
    }
}

geoMap.Tooltip = new MyTooltip();</code></pre>
