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

## Stroke property

Paints the outline of every land. When `null` (the default) no outline is
drawn — the heat fill alone defines each land's silhouette.

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

Paints lands that have **no value** in any series — the "background" lands
on the map. Lands that participate in a heat series keep their interpolated
heat color regardless of `Fill`. When `null` (the default) unmapped lands
stay transparent.

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

## Title property

A `Title` is a `VisualElement` rendered above the map. The same
`DrawnLabelVisual` used by cartesian and pie charts works here — set
`Text`, `TextSize`, `Padding`, and the `Paint` that draws it. The map
shrinks vertically to make room for the title.

{{~ if xaml ~}}
<pre><code>&lt;lvc:GeoMap Series="{Binding Series}"&gt;
    &lt;lvc:GeoMap.Title&gt;&lt;!-- mark -->
        &lt;lvc:XamlDrawnLabelVisual
            Text="World population by country"
            Paint="{lvc:SolidColorPaint Color='#303030'}"
            TextSize="20"
            Padding="{lvc:Padding '12'}"/&gt;
    &lt;/lvc:GeoMap.Title&gt;
&lt;/lvc:GeoMap></code></pre>
{{~ end ~}}

{{~ if blazor ~}}
<pre><code>&lt;GeoMap Series="series" Title="title"&gt;&lt;/GeoMap&gt;

@code {
    private DrawnLabelVisual title = new DrawnLabelVisual(
        new LabelGeometry
        {
            Text = "World population by country",
            TextSize = 20,
            Padding = new Padding(12),
            Paint = new SolidColorPaint(SKColors.Black)
        });
}</code></pre>
{{~ end ~}}

{{~ if winforms ~}}
<pre><code>geoMap1.Title = new DrawnLabelVisual(
    new LabelGeometry
    {
        Text = "World population by country",
        TextSize = 20,
        Padding = new Padding(12),
        Paint = new SolidColorPaint(SKColors.Black)
    });</code></pre>
{{~ end ~}}

## Legend property

Heat maps benefit from a gradient legend so the reader can map colors
back to values. Set `LegendPosition` and assign an `SKHeatLegend` —
the legend reads `HeatMap`, `ColorStops`, and the per-series
`WeightBounds` (min/max value across the data) to render the gradient
bar and its end labels.

| LegendPosition | Effect                                           |
| -------------- | ------------------------------------------------ |
| `Hidden`       | Default — no legend.                             |
| `Left`         | Vertical gradient bar pinned to the left.        |
| `Right`        | Vertical gradient bar pinned to the right.       |
| `Top`          | Horizontal gradient bar pinned to the top.      |
| `Bottom`       | Horizontal gradient bar pinned to the bottom.    |

{{~ if xaml ~}}
<pre><code>&lt;lvc:GeoMap
    Series="{Binding Series}"
    LegendPosition="Right"&gt;&lt;!-- mark -->
    &lt;lvc:GeoMap.Legend&gt;
        &lt;draw:SKHeatLegend BadgePadding="{lvc:Padding '20, 16, 8, 16'}"/&gt;&lt;!-- mark -->
    &lt;/lvc:GeoMap.Legend&gt;
&lt;/lvc:GeoMap></code></pre>

Make sure to declare the `draw` namespace on the root element:

<pre><code>xmlns:draw="using:LiveChartsCore.SkiaSharpView.SKCharts"</code></pre>
{{~ end ~}}

{{~ if blazor ~}}
<pre><code>&lt;GeoMap
    Series="series"
    LegendPosition="LiveChartsCore.Measure.LegendPosition.Right"
    Legend="legend"&gt;&lt;!-- mark -->
&lt;/GeoMap>

@code {
    private SKHeatLegend legend = new();
}</code></pre>
{{~ end ~}}

{{~ if winforms ~}}
<pre><code>geoMap1.LegendPosition = LiveChartsCore.Measure.LegendPosition.Right;
geoMap1.Legend = new SKHeatLegend(); // mark</code></pre>
{{~ end ~}}

:::info
Override the gradient endpoints (e.g. to pin the legend to 0–100 even
when the data only spans 5–87) by setting `MinValue` and `MaxValue`
on the `HeatLandSeries`. The map's heat ramp uses the same bounds, so
the rendered colors and the legend stay in sync.
:::

## MapProjection property

Defines the [projection](https://en.wikipedia.org/wiki/Map_projection) of the
map coordinates in the control coordinates. Three projections are available:

| Value          | Use case                                                            |
| -------------- | ------------------------------------------------------------------- |
| `Default`      | No projection — raw control-coordinate plot. Useful for non-geographic maps. |
| `Mercator`     | Flat world map; preserves angles, exaggerates polar areas.          |
| `Orthographic` | 3D globe view — only one hemisphere visible at a time, rotate to look at the other side. |

### Mercator

{{~ if xaml ~}}
<pre><code>&lt;lvc:GeoMap
    Series="{Binding Series}"
    MapProjection="Mercator"&gt;&lt;!-- mark -->
&lt;/lvc:GeoMap></code></pre>
{{~ end ~}}

{{~ if blazor ~}}
<pre><code>&lt;GeoMap
    Series="series"
    MapProjection="LiveChartsCore.Geo.MapProjection.Mercator"&gt;&lt;!-- mark -->
&lt;/GeoMap></code></pre>
{{~ end ~}}

{{~ if winforms ~}}
<pre><code>geoMap1.MapProjection = LiveChartsCore.Geo.MapProjection.Mercator;</code></pre>
{{~ end ~}}

![image](https://raw.githubusercontent.com/beto-rodriguez/LiveCharts2/master/docs/_assets/geomap-mercator.png)

By default the Mercator projection is clipped at ±65° latitude to drop
the sub-Antarctic empty band (and a sliver of Greenland). Each edge is
configurable via `MinLatitude`, `MaxLatitude`, `MinLongitude`, and
`MaxLongitude` on the chart — leave a value as `double.NaN` (the default)
to keep the projection's natural default.

Pass ±85° to render the classic full-earth Mercator including Antarctica:

{{~ if xaml ~}}
<pre><code>&lt;lvc:GeoMap
    Series="{Binding Series}"
    MapProjection="Mercator"
    MinLatitude="-85"
    MaxLatitude="85"/&gt;&lt;!-- mark, full earth --></code></pre>
{{~ end ~}}

{{~ if blazor ~}}
<pre><code>&lt;GeoMap
    Series="series"
    MapProjection="LiveChartsCore.Geo.MapProjection.Mercator"
    MinLatitude="-85"
    MaxLatitude="85"&gt;&lt;!-- mark, full earth -->
&lt;/GeoMap></code></pre>
{{~ end ~}}

{{~ if winforms ~}}
<pre><code>geoMap1.MinLatitude = -85; // mark
geoMap1.MaxLatitude = 85;  // mark, full earth</code></pre>
{{~ end ~}}

Combine all four bounds to focus the map on a region — e.g. central
Europe (`MinLatitude=35`, `MaxLatitude=72`, `MinLongitude=-15`,
`MaxLongitude=45`). Only the Mercator projection honors these today;
`Default` and `Orthographic` ignore them.

### Orthographic

`Orthographic` renders the map as a 3D globe — only the hemisphere facing the
camera is drawn, lands that cross the horizon are clipped along the disc rim.
`CoreChart.RotationX` (longitude) and `CoreChart.RotationY` (latitude) control
the center of view; setting them directly snaps, `CoreChart.RotateTo(lon, lat)`
animates.

{{~ if xaml ~}}
<pre><code>&lt;lvc:GeoMap
    x:Name="geoMap"
    Series="{Binding Series}"
    MapProjection="Orthographic"&gt;&lt;!-- mark -->
&lt;/lvc:GeoMap></code></pre>

<pre><code>// Code-behind / ViewModel: center the globe on Europe + Africa.
geoMap.CoreChart.RotationX = 15;  // longitude
geoMap.CoreChart.RotationY = 20;  // latitude</code></pre>
{{~ end ~}}

{{~ if blazor ~}}
<pre><code>&lt;GeoMap
    @ref="geoMap"
    Series="series"
    MapProjection="LiveChartsCore.Geo.MapProjection.Orthographic"&gt;&lt;!-- mark -->
&lt;/GeoMap>

@code {
    private GeoMap geoMap = null!;

    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender) return;
        geoMap.CoreChart.RotationX = 15;
        geoMap.CoreChart.RotationY = 20;
    }
}</code></pre>
{{~ end ~}}

{{~ if winforms ~}}
<pre><code>geoMap1.MapProjection = LiveChartsCore.Geo.MapProjection.Orthographic;
geoMap1.CoreChart.RotationX = 15;  // longitude
geoMap1.CoreChart.RotationY = 20;  // latitude</code></pre>
{{~ end ~}}

![image](https://raw.githubusercontent.com/beto-rodriguez/LiveCharts2/master/docs/_assets/geomap-orthographic.png)

Mouse-wheel zoom is supported the same way as on the flat projections; pan is
disabled by default — set `InteractionMode="Both"` to enable click-drag pan.

## InteractionMode property

Controls which user interactions the map responds to. Defaults to
`MapInteractionMode.None` — geo maps are most often embedded as static
dashboard tiles, so the default is no interaction. Set it to `Zoom`
for wheel-zoom only, `Pan` for click-drag pan only, or `Both` for both.

| Value  | Wheel zoom | Click-drag pan |
| ------ | ---------- | -------------- |
| `None` | ✗          | ✗ *(default)*  |
| `Pan`  | ✗          | ✓              |
| `Zoom` | ✓          | ✗              |
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

## Tooltip placement and styling

The `TooltipPosition` property controls where the popup anchors relative to the
hovered land's centroid. The map auto-flips between top and bottom when
`Auto` is set and the popup would clip the chart edge.

| Value    | Behavior                                            |
| -------- | --------------------------------------------------- |
| `Auto`   | Default — places above the land, flips below near the top edge. |
| `Top`    | Always above the land (wedge points down).          |
| `Bottom` | Always below the land (wedge points up).            |
| `Hidden` | Disables the tooltip entirely.                      |

{{~ if xaml ~}}
<pre><code>&lt;lvc:GeoMap
    Series="{Binding Series}"
    TooltipPosition="Top"&gt;&lt;!-- mark -->
&lt;/lvc:GeoMap></code></pre>
{{~ end ~}}

{{~ if blazor ~}}
<pre><code>&lt;GeoMap
    Series="series"
    TooltipPosition="LiveChartsCore.Measure.TooltipPosition.Top"&gt;&lt;!-- mark -->
&lt;/GeoMap></code></pre>
{{~ end ~}}

{{~ if winforms ~}}
<pre><code>geoMap1.TooltipPosition = LiveChartsCore.Measure.TooltipPosition.Top;</code></pre>
{{~ end ~}}

`TooltipTextSize`, `TooltipTextPaint`, and `TooltipBackgroundPaint` style the
default tooltip without replacing it. `TooltipTextSize` defaults to the active
theme; `TooltipTextPaint` and `TooltipBackgroundPaint` fall back to theme
paints when null.

{{~ if xaml ~}}
<pre><code>&lt;lvc:GeoMap
    Series="{Binding Series}"
    TooltipTextSize="16"&gt;&lt;!-- mark -->
&lt;/lvc:GeoMap></code></pre>
{{~ end ~}}

{{~ if blazor ~}}
<pre><code>&lt;GeoMap
    Series="series"
    TooltipTextSize="16"&gt;&lt;!-- mark -->
&lt;/GeoMap></code></pre>
{{~ end ~}}

{{~ if winforms ~}}
<pre><code>geoMap1.TooltipTextSize = 16;
geoMap1.TooltipTextPaint = new SolidColorPaint(SKColors.White);
geoMap1.TooltipBackgroundPaint = new SolidColorPaint(SKColors.Black);</code></pre>
{{~ end ~}}

## Programmatic zoom, pan and reset

`MapInteractionMode` covers user gestures; the methods on `CoreChart` cover
programmatic viewport control:

- `CoreChart.ResetViewport()` — snap back to zoom 1.0 / no pan.
- `CoreChart.Pan(LvcPoint delta)` — pan by a screen-space offset.
- `CoreChart.Zoom(LvcPoint pivot, ZoomDirection direction)` — zoom in / out
  around a screen point.
- `CoreChart.RotateTo(double longitude, double latitude, int durationMs = 800)`
  — animated globe rotation for `MapProjection.Orthographic`. `RotationX` and
  `RotationY` set rotation without animation.

<pre><code>// Reset zoom and pan to defaults.
geoMap.CoreChart.ResetViewport();

// Animate the orthographic globe to look at Tokyo.
geoMap.CoreChart.RotateTo(longitude: 139.69, latitude: 35.69);</code></pre>

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
