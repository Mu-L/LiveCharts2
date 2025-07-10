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

using System;
using System.Linq;
using LiveChartsCore.Drawing;
using LiveChartsCore.Painting;
using LiveChartsCore.SkiaSharpView.Drawing;
using SkiaSharp;

namespace LiveChartsCore.SkiaSharpView.Painting;

/// <summary>
/// Defines a set of geometries that will be painted using a linear gradient shader.
/// </summary>
/// <seealso cref="SkiaPaint" />
/// <remarks>
/// Initializes a new instance of the <see cref="LinearGradientPaint"/> class.
/// </remarks>
/// <param name="gradientStops">The gradient stops.</param>
/// <param name="startPoint">
/// The start point, both X and Y in the range of 0 to 1, where 0 is the start of the axis and 1 the end.
/// </param>
/// <param name="endPoint">
/// The end point, both X and Y in the range of 0 to 1, where 0 is the start of the axis and 1 the end.
/// </param>
/// <param name="colorPos">
/// An array of floats in the range of 0 to 1.
/// These floats indicate the relative positions of the colors, you can set that argument to null to equally
/// space the colors, default is null.
/// </param>
/// <param name="tileMode">
/// The shader tile mode, default is <see cref="SKShaderTileMode.Repeat"/>.
/// </param>
public class LinearGradientPaint(
    SKColor[] gradientStops,
    SKPoint startPoint,
    SKPoint endPoint,
    float[]? colorPos = null,
    SKShaderTileMode tileMode = SKShaderTileMode.Repeat)
        : SkiaPaint
{
    private SkiaSharpDrawingContext? _drawingContext;
    private SKPaint? _skiaPaint;

    private SKColor[] GradientStops { get; set; } = gradientStops;
    private SKPoint StartPoint { get; set; } = startPoint;
    private SKPoint EndPoint { get; set; } = endPoint;
    private float[]? ColorPos { get; set; } = colorPos;

    /// <summary>
    /// Default start point.
    /// </summary>
    public static readonly SKPoint DefaultStartPoint = new(0, 0.5f);

    /// <summary>
    /// Default end point.
    /// </summary>
    public static readonly SKPoint DefaultEndPoint = new(1, 0.5f);

    /// <summary>
    /// Initializes a new instance of the <see cref="LinearGradientPaint"/> class.
    /// </summary>
    /// <param name="gradientStops">The gradient stops.</param>
    public LinearGradientPaint(SKColor[] gradientStops)
        : this(gradientStops, DefaultStartPoint, DefaultEndPoint) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LinearGradientPaint"/> class.
    /// </summary>
    /// <param name="startColor">The start color.</param>
    /// <param name="endColor">The end color.</param>
    /// <param name="startPoint">
    /// The start point, both X and Y in the range of 0 to 1, where 0 is the start of the axis and 1 the end.
    /// </param>
    /// <param name="endPoint">
    /// The end point, both X and Y in the range of 0 to 1, where 0 is the start of the axis and 1 the end.
    /// </param>
    public LinearGradientPaint(SKColor startColor, SKColor endColor, SKPoint startPoint, SKPoint endPoint)
        : this([startColor, endColor], startPoint, endPoint) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LinearGradientPaint"/> class.
    /// </summary>
    /// <param name="start">The start.</param>
    /// <param name="end">The end.</param>
    public LinearGradientPaint(SKColor start, SKColor end)
        : this(start, end, DefaultStartPoint, DefaultEndPoint) { }

    /// <inheritdoc cref="Paint.CloneTask" />
    public override Paint CloneTask()
    {
        return new LinearGradientPaint(GradientStops, StartPoint, EndPoint, ColorPos, tileMode)
        {
            PaintStyle = PaintStyle,
            IsAntialias = IsAntialias,
            StrokeThickness = StrokeThickness,
            StrokeCap = StrokeCap,
            StrokeJoin = StrokeJoin,
            StrokeMiter = StrokeMiter,
            FontFamily = FontFamily,
            SKFontStyle = SKFontStyle,
            SKTypeface = SKTypeface,
            PathEffect = PathEffect?.Clone(),
            ImageFilter = ImageFilter?.Clone()
        };
    }

    /// <inheritdoc cref="Paint.ApplyOpacityMask(DrawingContext, float)" />
    public override void ApplyOpacityMask(DrawingContext context, float opacity)
    {
        if (_skiaPaint is null) return;
        var skiaContext = (SkiaSharpDrawingContext)context;

        var size = GetDrawRectangleSize(skiaContext);

        var xf = size.Location.X;
        var xt = xf + size.Width;

        var yf = size.Location.Y;
        var yt = yf + size.Height;

        var start = new SKPoint(xf + (xt - xf) * StartPoint.X, yf + (yt - yf) * StartPoint.Y);
        var end = new SKPoint(xf + (xt - xf) * EndPoint.X, yf + (yt - yf) * EndPoint.Y);

        _skiaPaint.Shader = SKShader.CreateLinearGradient(
            start,
            end,
            [.. GradientStops.Select(x => new SKColor(x.Red, x.Green, x.Blue, (byte)(255 * opacity)))],
            ColorPos,
            tileMode);
    }

    /// <inheritdoc cref="Paint.RestoreOpacityMask(DrawingContext, float)" />
    public override void RestoreOpacityMask(DrawingContext context, float opacity)
    {
        if (_skiaPaint is null) return;

        var size = GetDrawRectangleSize((SkiaSharpDrawingContext)context);

        var xf = size.Location.X;
        var xt = xf + size.Width;

        var yf = size.Location.Y;
        var yt = yf + size.Height;

        var start = new SKPoint(xf + (xt - xf) * StartPoint.X, yf + (yt - yf) * StartPoint.Y);
        var end = new SKPoint(xf + (xt - xf) * EndPoint.X, yf + (yt - yf) * EndPoint.Y);

        _skiaPaint.Shader = SKShader.CreateLinearGradient(
            start,
            end,
            GradientStops,
            ColorPos,
            tileMode);
    }

    /// <inheritdoc cref="Paint.InitializeTask(DrawingContext)" />
    public override void InitializeTask(DrawingContext drawingContext)
    {
        var skiaContext = (SkiaSharpDrawingContext)drawingContext;
        _skiaPaint ??= new SKPaint();

        var size = GetDrawRectangleSize(skiaContext);

        var xf = size.Location.X;
        var xt = xf + size.Width;

        var yf = size.Location.Y;
        var yt = yf + size.Height;

        var start = new SKPoint(xf + (xt - xf) * StartPoint.X, yf + (yt - yf) * StartPoint.Y);
        var end = new SKPoint(xf + (xt - xf) * EndPoint.X, yf + (yt - yf) * EndPoint.Y);

        _skiaPaint.Shader = SKShader.CreateLinearGradient(
            start,
            end,
            GradientStops,
            ColorPos,
            tileMode);

        _skiaPaint.IsAntialias = IsAntialias;
        _skiaPaint.StrokeWidth = StrokeThickness;
        _skiaPaint.StrokeCap = StrokeCap;
        _skiaPaint.StrokeJoin = StrokeJoin;
        _skiaPaint.StrokeMiter = StrokeMiter;
        _skiaPaint.Style = PaintStyle.HasFlag(PaintStyle.Stroke) ? SKPaintStyle.Stroke : SKPaintStyle.Fill;

        if (HasCustomFont) _skiaPaint.Typeface = GetSKTypeface();

        if (PathEffect is not null)
        {
            PathEffect.CreateEffect(skiaContext);
            _skiaPaint.PathEffect = PathEffect.SKPathEffect;
        }

        if (ImageFilter is not null)
        {
            ImageFilter.CreateFilter(skiaContext);
            _skiaPaint.ImageFilter = ImageFilter.SKImageFilter;
        }

        var clip = GetClipRectangle(skiaContext.MotionCanvas);
        if (clip != LvcRectangle.Empty)
        {
            _ = skiaContext.Canvas.Save();
            skiaContext.Canvas.ClipRect(new SKRect(clip.X, clip.Y, clip.X + clip.Width, clip.Y + clip.Height));
            _drawingContext = skiaContext;
        }

        skiaContext.ActiveSkiaPaint = _skiaPaint;
    }

    /// <inheritdoc cref="Paint.Transitionate(float, Paint)"/>
    public override Paint Transitionate(float progress, Paint target)
    {
        if (target._source is not LinearGradientPaint paint) return target;

        var clone = (LinearGradientPaint)CloneTask();

        clone.StrokeThickness = StrokeThickness + progress * (paint.StrokeThickness - StrokeThickness);
        clone.StrokeMiter = StrokeMiter + progress * (paint.StrokeMiter - StrokeMiter);
        clone.PathEffect = PathEffect?.Transitionate(progress, paint.PathEffect);
        clone.ImageFilter = ImageFilters.ImageFilter.Transitionate(ImageFilter, paint.ImageFilter, progress);

        if (paint.GradientStops.Length != GradientStops.Length)
            throw new NotImplementedException(
                $"Transitions between {nameof(GradientStops)} must be of the same length.");

        for (var i = 0; i < GradientStops.Length; i++)
            clone.GradientStops[i] = new SKColor(
                (byte)(GradientStops[i].Red + progress * (paint.GradientStops[i].Red - GradientStops[i].Red)),
                (byte)(GradientStops[i].Green + progress * (paint.GradientStops[i].Green - GradientStops[i].Green)),
                (byte)(GradientStops[i].Blue + progress * (paint.GradientStops[i].Blue - GradientStops[i].Blue)),
                (byte)(GradientStops[i].Alpha + progress * (paint.GradientStops[i].Alpha - GradientStops[i].Alpha)));

        clone.StartPoint = new SKPoint(
            StartPoint.X + progress * (paint.StartPoint.X - StartPoint.X),
            StartPoint.Y + progress * (paint.StartPoint.Y - StartPoint.Y));

        clone.EndPoint = new SKPoint(
            EndPoint.X + progress * (paint.EndPoint.X - EndPoint.X),
            EndPoint.Y + progress * (paint.EndPoint.Y - EndPoint.Y));

        if (ColorPos is not null && paint.ColorPos is not null)
        {
            if (clone.ColorPos is null || ColorPos.Length != paint.ColorPos.Length)
                throw new NotImplementedException(
                    $"Transitions between {nameof(ColorPos)} must be of the same length.");

            for (var i = 0; i < ColorPos.Length; i++)
                clone.ColorPos[i] = ColorPos[i] + progress * (paint.ColorPos[i] - ColorPos[i]);
        }

        return clone;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public override void Dispose()
    {
        // Note #301222
        // Disposing typefaces could cause render issues.
        // Does this causes memory leaks?
        // Should the user dispose typefaces manually?
        //if (HasCustomFont && _skiaPaint != null) _skiaPaint.Typeface.Dispose();
        PathEffect?.Dispose();
        ImageFilter?.Dispose();

        if (_drawingContext is not null && GetClipRectangle(_drawingContext.MotionCanvas) != LvcRectangle.Empty)
        {
            _drawingContext.Canvas.Restore();
            _drawingContext = null;
        }

        _skiaPaint?.Dispose();
        _skiaPaint = null;

        GC.SuppressFinalize(this);
    }

    private static SKRect GetDrawRectangleSize(SkiaSharpDrawingContext drawingContext) =>
        new(0, 0, drawingContext.Info.Width, drawingContext.Info.Height);
}
