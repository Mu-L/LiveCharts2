// The MIT License(MIT)
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

using System;

namespace LiveChartsCore.Geo;

/// <summary>
/// Projects latitude and longitude coordinates using the Mercator projection.
/// </summary>
/// <seealso cref="MapProjector" />
public class MercatorProjector : MapProjector
{
    /// <summary>
    /// The default maximum latitude (in degrees) for the Mercator projection.
    /// Standard Mercator extends to ±85° but anything beyond about ±65° is
    /// inhabited by penguins; clipping there fills the canvas with the
    /// continents and removes the empty band beneath Antarctica.
    /// </summary>
    public const double DefaultMaxLatitudeDegrees = 65d;

    private readonly float _w;
    private readonly float _h;
    private readonly float _ox;
    private readonly float _oy;
    private readonly double _maxLatMercN;

    /// <summary>
    /// Initializes a new instance of the <see cref="MercatorProjector"/> class
    /// with the default <see cref="DefaultMaxLatitudeDegrees"/> latitude clip.
    /// </summary>
    public MercatorProjector(float mapWidth, float mapHeight, float offsetX, float offsetY)
        : this(mapWidth, mapHeight, offsetX, offsetY, DefaultMaxLatitudeDegrees)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MercatorProjector"/> class.
    /// </summary>
    /// <param name="mapWidth">Width of the map.</param>
    /// <param name="mapHeight">Height of the map.</param>
    /// <param name="offsetX">The offset x.</param>
    /// <param name="offsetY">The offset y.</param>
    /// <param name="maxLatitudeDegrees">The latitude (in degrees) clipped to the
    /// top and bottom edges. Defaults to <see cref="DefaultMaxLatitudeDegrees"/>;
    /// pass 85 to render the full standard Mercator including Antarctica.</param>
    public MercatorProjector(float mapWidth, float mapHeight, float offsetX, float offsetY, double maxLatitudeDegrees)
    {
        _w = mapWidth;
        _h = mapHeight;
        _ox = offsetX;
        _oy = offsetY;
        _maxLatMercN = MercN(maxLatitudeDegrees);
        XOffset = _ox;
        YOffset = _oy;
        MapWidth = mapWidth;
        MapHeight = mapHeight;
    }

    /// <summary>
    /// Gets the preferred ratio for the default Mercator latitude clip.
    /// Use <see cref="GetPreferredRatio(double)"/> for a custom clip.
    /// </summary>
    public static float[] PreferredRatio => GetPreferredRatio(DefaultMaxLatitudeDegrees);

    /// <summary>
    /// Returns the natural aspect ratio of the projection clipped at the given
    /// latitude — wider than tall for smaller clips, square at ±85°.
    /// </summary>
    public static float[] GetPreferredRatio(double maxLatitudeDegrees) =>
        new[] { (float)(Math.PI / MercN(maxLatitudeDegrees)), 1f };

    /// <inheritdoc cref="MapProjector.ToMap(double[])"/>
    public override float[] ToMap(double[] point)
    {
        ToMap(point[0], point[1], out var x, out var y);
        return [x, y];
    }

    /// <inheritdoc cref="MapProjector.ToMap(double, double, out float, out float)"/>
    public override void ToMap(double longitude, double latitude, out float x, out float y)
    {
        var mercN = MercN(latitude);
        // Scale by the clip's mercN (not π = mercN(85°)) so ±maxLatitudeDegrees
        // spans the full _h; lands beyond the clip are extrapolated and will
        // fall off the edge.
        var py = _h / 2d - _h * mercN / (2 * _maxLatMercN);

        x = (float)((longitude + 180) * (_w / 360d) + _ox);
        y = (float)py + _oy;
    }

    private static double MercN(double latitudeDegrees)
    {
        var latRad = latitudeDegrees * Math.PI / 180d;
        return Math.Log(Math.Tan(Math.PI / 4d + latRad / 2d), Math.E);
    }
}
