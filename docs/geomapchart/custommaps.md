<!--
To get help on editing this file, see https://github.com/beto-rodriguez/LiveCharts2/blob/master/docs/readme.md
-->

# Custom maps

The `GeoMap` ships with a world map (~110 countries) baked in. To render
anything else — country subdivisions like states or provinces, a custom
region, a sport-event venue layout — point the chart at your own GeoJSON
file and assign it to `GeoMap.ActiveMap`.

This article walks through loading a custom map. The runnable sample is
`Maps/CustomMap` in the samples gallery; the sample's
[mexico-states.geojson](https://github.com/Live-Charts/LiveCharts2/blob/master/samples/ViewModelsSamples/Maps/CustomMap/mexico-states.geojson)
shows the file format the library expects.

![image](https://raw.githubusercontent.com/Live-Charts/LiveCharts2/refs/heads/master/tests/SnapshotTests/Snapshots/MapsTests_CustomMap_LoadsAndRendersMexicoStates.png)

## Where to get GeoJSON data

The library has no opinion about where your GeoJSON comes from — anything
that parses as a `FeatureCollection` works. The CustomMap sample uses
[Natural Earth Data](https://www.naturalearthdata.com/) (CC0 / public
domain), the standard open-source source for world-and-country maps.
Convenient mirrors:

- [martynafford/natural-earth-geojson](https://github.com/martynafford/natural-earth-geojson)
  — Natural Earth's shapefiles already converted to GeoJSON, CC0.
- [geojson.xyz](https://geojson.xyz/) — hosted GeoJSON tiles of
  Natural Earth and other public-domain sources.

Other clean sources include OpenStreetMap exports (ODbL — attribution
required) and your country's open-data portal. Avoid GADM and similar
non-commercial-only datasets for any product you intend to ship.

:::info
**A note on disputed boundaries.** Map data inherits the borders chosen
by whoever drew the source — there are real disagreements about
Crimea, Taiwan, Western Sahara, Kashmir, and other regions, and no
single GeoJSON satisfies every audience. LiveCharts2 doesn't take a
position; it just renders the GeoJSON you give it. If the borders
shown don't match what you need, load a different GeoJSON.
:::

## File format

LiveCharts2 expects a standard GeoJSON `FeatureCollection`. Each
`Feature` is one land you'll be able to color via `HeatLandSeries`. The
library reads three optional properties off each feature:

```json
{
  "type": "Feature",
  "properties": {
    "name": "Jalisco",
    "shortName": "jal",
    "setOf": "mexico"
  },
  "geometry": { "type": "MultiPolygon", "coordinates": [ /* ... */ ] }
}
```

- `name` — display label (used by tooltips).
- `shortName` — the **key** `HeatLand.Name` matches against. Pick a
  stable, lowercase, URL-safe code; for administrative subdivisions, the
  ISO 3166-2 region code without the country prefix is a good default
  (e.g. `jal` for `MX-JAL`).
- `setOf` — optional logical grouping, free-form (e.g. `"mexico"` or
  `"eu"`). Unused by core rendering but available on the
  `LandDefinition` for downstream filtering.

Coordinates use standard GeoJSON [longitude, latitude] order. The
parser accepts `Polygon` and `MultiPolygon` geometries.

## Loading the file

Three loaders on the static `LiveChartsCore.Geo.Maps` class:

<pre><code>// 1) From a file on disk:
var mexicoMap = Maps.GetMapFromDirectory(@"C:\maps\mexico-states.geojson");

// 2) From any Stream / StreamReader (most flexible — works for embedded
//    resources, HTTP downloads, in-memory bytes, etc.):
using var reader = new StreamReader(geoJsonStream);
var mexicoMap = Maps.GetMapFromStreamReader(reader);

// 3) Direct constructor on DrawnMap when you need named layers:
var mexicoMap = new DrawnMap(reader, layerName: "mexico");

geoMap.ActiveMap = mexicoMap;
</code></pre>

For a sample shipping a GeoJSON inside the assembly, embed it as a
build resource and load via `Assembly.GetManifestResourceStream`:

{{~ if xaml ~}}
<pre><code>using System.IO;
using System.Reflection;
using LiveChartsCore.Geo;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;

namespace ViewModelsSamples.Maps.CustomMap
{
    public class ViewModel
    {
        public ViewModel()
        {
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("ViewModelsSamples.Maps.CustomMap.mexico-states.geojson")!;
            using var reader = new StreamReader(stream);
            ActiveMap = Maps.GetMapFromStreamReader(reader);

            Series = [
                new HeatLandSeries
                {
                    Lands = [
                        new HeatLand { Name = "cmx", Value = 92 }, // Ciudad de México
                        new HeatLand { Name = "jal", Value = 84 }, // Jalisco
                        new HeatLand { Name = "nle", Value = 58 }, // Nuevo León
                        // ... 32 entities total ...
                    ],
                },
            ];
        }

        public DrawnMap ActiveMap { get; }
        public HeatLandSeries[] Series { get; }
    }
}</code></pre>

<pre><code>&lt;lvc:GeoMap
    Series="{Binding Series}"
    ActiveMap="{Binding ActiveMap}"&gt;&lt;!-- mark -->
&lt;/lvc:GeoMap></code></pre>

The csproj registers the GeoJSON as an embedded resource so every
platform picks it up automatically without per-platform asset wiring:

<pre><code>&lt;ItemGroup&gt;
    &lt;EmbeddedResource Include="Maps\CustomMap\mexico-states.geojson" /&gt;
&lt;/ItemGroup></code></pre>
{{~ end ~}}

{{~ if blazor ~}}
<pre><code>@using System.IO
@using System.Reflection
@using LiveChartsCore.Geo
@using LiveChartsCore.SkiaSharpView

&lt;GeoMap Series="series" ActiveMap="activeMap"&gt;&lt;/GeoMap>

@code {
    private DrawnMap activeMap;
    private HeatLandSeries[] series;

    protected override void OnInitialized()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("YourApp.Maps.mexico-states.geojson")!;
        using var reader = new StreamReader(stream);
        activeMap = Maps.GetMapFromStreamReader(reader);

        series = [
            new HeatLandSeries
            {
                Lands = [
                    new HeatLand { Name = "cmx", Value = 92 },
                    // ...
                ],
            },
        ];
    }
}</code></pre>
{{~ end ~}}

{{~ if winforms ~}}
<pre><code>using var stream = Assembly.GetExecutingAssembly()
    .GetManifestResourceStream("YourApp.Maps.mexico-states.geojson")!;
using var reader = new StreamReader(stream);

geoMap1.ActiveMap = Maps.GetMapFromStreamReader(reader);
geoMap1.Series = new HeatLandSeries[]
{
    new() {
        Lands = new HeatLand[] {
            new() { Name = "cmx", Value = 92 },
            // ...
        }
    }
};</code></pre>
{{~ end ~}}

## Frame the chart on your region

Custom maps come in any shape, so the default world bounds usually
letterbox your data into a sliver. Set the four lat/lon bounds on the
chart to frame on the region — Mexico fits well in roughly
`(lon -120..-85, lat 13..33)`:

{{~ if xaml ~}}
<pre><code>&lt;lvc:GeoMap
    Series="{Binding Series}"
    ActiveMap="{Binding ActiveMap}"
    MinLatitude="13"
    MaxLatitude="33"
    MinLongitude="-120"
    MaxLongitude="-85"/&gt;</code></pre>
{{~ end ~}}

See the [GeoMap overview](overview) article for the full bounds API.

## Multi-layer maps

`DrawnMap.AddLayerFromStreamReader(streamReader, layerName)` and
`AddLayerFromDirectory(path, layerName)` add additional GeoJSON
layers on top of the base map — useful for overlays like state-level
detail on a country base, river networks over a region, etc. Each
layer is independent; `HeatLandSeries` matches by `shortName` across
all layers.
