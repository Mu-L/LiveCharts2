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
using LiveChartsCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests.CoreObjectsTests;

// The cubic-bezier easing now precomputes the curve into a lookup table and interpolates at
// runtime instead of solving x(t) per call. These tests pin the LUT output to the exact analytic
// curve (a high-iteration double-precision Newton solve) so the optimization cannot drift.
[TestClass]
public class CubicBezierEasingTests
{
    // Exact reference: solve x(t) == x with many Newton steps in double precision, then return y(t).
    private static double Reference(double x, double x1, double x2, double y1, double y2)
    {
        static double Calc(double t, double a1, double a2) =>
            (((1 - 3 * a2 + 3 * a1) * t + (3 * a2 - 6 * a1)) * t + 3 * a1) * t;
        static double Slope(double t, double a1, double a2) =>
            (3 * (1 - 3 * a2 + 3 * a1) * t + 2 * (3 * a2 - 6 * a1)) * t + 3 * a1;

        if (x <= 0) return 0;
        if (x >= 1) return 1;

        var t = x;
        for (var i = 0; i < 60; i++)
        {
            var slope = Slope(t, x1, x2);
            if (slope == 0) break;
            t -= (Calc(t, x1, x2) - x) / slope;
        }
        return Calc(t, y1, y2);
    }

    [DataTestMethod]
    [DataRow(0.25f, 0.1f, 0.25f, 1f)]  // Ease
    [DataRow(0.42f, 0f, 1f, 1f)]       // EaseIn
    [DataRow(0f, 0f, 0.58f, 1f)]       // EaseOut
    [DataRow(0.42f, 0f, 0.58f, 1f)]    // EaseInOut
    [DataRow(0.8f, 0f, 0.2f, 1f)]      // steep, monotonic custom curve
    public void LutMatchesAnalyticAcrossDomain(float x1, float y1, float x2, float y2)
    {
        var f = EasingFunctions.BuildCubicBezier(x1, y1, x2, y2);

        // endpoints are exact by construction.
        Assert.AreEqual(0f, f(0f), 1e-4f);
        Assert.AreEqual(1f, f(1f), 1e-4f);

        var previous = float.NegativeInfinity;
        for (var i = 0; i <= 200; i++)
        {
            var x = i / 200f;
            var actual = f(x);
            var expected = (float)Reference(x, x1, x2, y1, y2);

            Assert.IsTrue(
                Math.Abs(actual - expected) < 5e-3f,
                $"x={x}: LUT {actual} differs from analytic {expected} by more than 5e-3.");

            // these curves are monotonic non-decreasing; the LUT must preserve that.
            Assert.IsTrue(
                actual >= previous - 1e-4f,
                $"easing must be monotonic non-decreasing (x={x}).");
            previous = actual;
        }
    }

    [TestMethod]
    public void LinearShortcutIsReturnedForIdentityCurve()
    {
        // when the control points lie on the diagonal the curve is the identity; the builder
        // should return the linear shortcut rather than a table.
        var f = EasingFunctions.BuildCubicBezier(0.3f, 0.3f, 0.7f, 0.7f);
        for (var i = 0; i <= 10; i++)
        {
            var x = i / 10f;
            Assert.AreEqual(x, f(x), 1e-6f);
        }
    }
}
