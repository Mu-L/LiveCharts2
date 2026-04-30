using System.IO;
using System.Linq;
using LiveChartsCore.Geo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests.OtherTests;

[TestClass]
public class GeoJsonImportTests
{
    // Standard GeoJson allowed by RFC 7946: mixed Polygon / MultiPolygon geometries,
    // numeric and string property values, and no LiveCharts-specific keys
    // (no "shortName", no "setOf"). Regression for issue #1426.
    private const string CustomFeatureCollection = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "properties": { "name": "Piemonte", "code_num": 1 },
              "geometry": {
                "type": "Polygon",
                "coordinates": [[[0,0],[1,0],[1,1],[0,1],[0,0]]]
              }
            },
            {
              "type": "Feature",
              "properties": { "name": "Sicilia", "code_num": 19 },
              "geometry": {
                "type": "MultiPolygon",
                "coordinates": [[[[10,10],[11,10],[11,11],[10,11],[10,10]]]]
              }
            },
            {
              "type": "Feature",
              "properties": { "label": "no-name-key" },
              "geometry": {
                "type": "Polygon",
                "coordinates": [[[5,5],[6,5],[6,6],[5,5]]]
              }
            },
            {
              "type": "Feature",
              "properties": { "name": "Capital" },
              "geometry": { "type": "Point", "coordinates": [0,0] }
            },
            {
              "type": "Feature",
              "properties": { "name": "Highway" },
              "geometry": {
                "type": "MultiLineString",
                "coordinates": [[[20,20],[21,21]],[[22,22],[23,23]]]
              }
            }
          ]
        }
        """;

    [TestMethod]
    public void Import_CustomGeoJson_ParsesMixedPolygonShapes()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(CustomFeatureCollection));
        using var sr = new StreamReader(stream);

        var map = Maps.GetMapFromStreamReader(sr);

        Assert.IsTrue(map.Layers.TryGetValue("default", out var layer), "default layer should exist");
        // Polygon + MultiPolygon land, plus the missing-name fallback. Point and
        // MultiLineString are non-area types and must be skipped (MultiLineString
        // shares 3-deep coordinate shape with Polygon, so dispatching on type matters).
        Assert.AreEqual(3, layer!.Lands.Count, "polygon-shaped features should be loaded; non-area types skipped");

        Assert.IsNotNull(map.FindLand("piemonte"), "Polygon feature should be found by lower-cased name");
        Assert.IsNotNull(map.FindLand("sicilia"), "MultiPolygon feature should be found by lower-cased name");
        // The unnamed feature falls back to a synthetic feature_<index> identifier.
        Assert.IsTrue(layer.Lands.Keys.Any(k => k.StartsWith("feature_")), "missing-name feature should fall back to feature_<i>");
    }

    [TestMethod]
    public void Import_BuiltInWorldMap_StillLoads()
    {
        // Sanity check that the LiveCharts-shipped world.geojson keeps loading after
        // the parser changes (its features carry "shortName" + "setOf" properties).
        var world = Maps.GetWorldMap();
        Assert.IsTrue(world.Layers["default"].Lands.Count > 100, "world map lands should still load");
        Assert.IsNotNull(world.FindLand("bra"), "Brazil should be found by shortName");
    }
}
