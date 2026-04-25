using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace industREAL.HMI.Sample.BasicServo.Controls;

public class CircularGauge : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<CircularGauge, double>(nameof(Value));

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<CircularGauge, double>(nameof(Minimum), -32768);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<CircularGauge, double>(nameof(Maximum), 32767);

    public static readonly StyledProperty<double> StrokeWidthProperty =
        AvaloniaProperty.Register<CircularGauge, double>(nameof(StrokeWidth), 12);

    public static readonly StyledProperty<IBrush> TrackBrushProperty =
        AvaloniaProperty.Register<CircularGauge, IBrush>(nameof(TrackBrush), new SolidColorBrush(Color.FromRgb(60, 60, 60)));

    public static readonly StyledProperty<IBrush> FillBrushProperty =
        AvaloniaProperty.Register<CircularGauge, IBrush>(nameof(FillBrush), new SolidColorBrush(Color.FromRgb(0, 120, 215)));

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double StrokeWidth
    {
        get => GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
    }

    public IBrush TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public IBrush FillBrush
    {
        get => GetValue(FillBrushProperty);
        set => SetValue(FillBrushProperty, value);
    }

    static CircularGauge()
    {
        AffectsRender<CircularGauge>(ValueProperty, MinimumProperty, MaximumProperty,
            StrokeWidthProperty, TrackBrushProperty, FillBrushProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double size = Math.Min(Bounds.Width, Bounds.Height);
        double radius = (size - StrokeWidth) / 2;
        if (radius <= 0) return;

        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var pen = new Pen(TrackBrush, StrokeWidth, lineCap: PenLineCap.Round);

        // Draw track (full circle)
        context.DrawEllipse(null, pen, center, radius, radius);

        // Calculate sweep angle from value
        double value = Math.Clamp(Value, Minimum, Maximum);
        if (Math.Abs(value) < 0.001) return;

        double sweepDegrees;
        if (value > 0)
            sweepDegrees = (value / Maximum) * 180.0;
        else
            sweepDegrees = (value / Minimum) * -180.0; // negative sweep for CCW

        double startAngleRad = -Math.PI / 2; // 12 o'clock
        double sweepRad = sweepDegrees * Math.PI / 180.0;

        var startPoint = new Point(
            center.X + radius * Math.Cos(startAngleRad),
            center.Y + radius * Math.Sin(startAngleRad));

        var endPoint = new Point(
            center.X + radius * Math.Cos(startAngleRad + sweepRad),
            center.Y + radius * Math.Sin(startAngleRad + sweepRad));

        bool isLargeArc = Math.Abs(sweepDegrees) > 180;
        var sweepDirection = sweepDegrees >= 0
            ? SweepDirection.Clockwise
            : SweepDirection.CounterClockwise;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(startPoint, false);
            ctx.ArcTo(endPoint, new Size(radius, radius), 0, isLargeArc, sweepDirection);
        }

        var fillPen = new Pen(FillBrush, StrokeWidth, lineCap: PenLineCap.Round);
        context.DrawGeometry(null, fillPen, geometry);
    }
}
