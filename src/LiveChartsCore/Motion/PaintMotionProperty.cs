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

using LiveChartsCore.Painting;

namespace LiveChartsCore.Motion;

/// <summary>
/// Defines a motion property that holds a <see cref="Paint"/> reference.
/// </summary>
/// <remarks>
/// A paint reference is not interpolated: changing the reference snaps to the new paint.
/// Animation now lives inside the paint itself (its own properties are motion properties,
/// e.g. <c>SolidColorPaint.Color</c>), so a paint animates by mutating its own state
/// on the same instance rather than by blending one paint instance into another.
/// </remarks>
/// <param name="defaultValue">The default value.</param>
public class PaintMotionProperty(Paint defaultValue = null!)
    : MotionProperty<Paint?>(defaultValue)
{
    /// <inheritdoc cref="MotionProperty{T}.CanTransitionate"/>
    protected override bool CanTransitionate => false;

    /// <inheritdoc cref="MotionProperty{T}.OnGetMovement(float)" />
    // Never invoked: CanTransitionate is false, so GetMovement returns the value directly.
    protected override Paint? OnGetMovement(float progress) => ToValue;
}
