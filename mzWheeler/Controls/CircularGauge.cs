using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace mzWheeler.Controls;

public class CircularGauge : GraphicsView
{
    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(double), typeof(CircularGauge), 0.0,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                ((CircularGauge)bindable).Invalidate();
            });

    public static readonly BindableProperty MinValueProperty =
        BindableProperty.Create(nameof(MinValue), typeof(double), typeof(CircularGauge), 0.0,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                ((CircularGauge)bindable).Invalidate();
            });

    public static readonly BindableProperty MaxValueProperty =
        BindableProperty.Create(nameof(MaxValue), typeof(double), typeof(CircularGauge), 100.0,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                ((CircularGauge)bindable).Invalidate();
            });

    public static readonly BindableProperty ArcColorProperty =
        BindableProperty.Create(nameof(ArcColor), typeof(Color), typeof(CircularGauge), Colors.Green,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                ((CircularGauge)bindable).Invalidate();
            });

    public static readonly BindableProperty BackgroundArcColorProperty =
        BindableProperty.Create(nameof(BackgroundArcColor), typeof(Color), typeof(CircularGauge), Colors.Gray,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                ((CircularGauge)bindable).Invalidate();
            });

    public static readonly BindableProperty ArcThicknessProperty =
        BindableProperty.Create(nameof(ArcThickness), typeof(float), typeof(CircularGauge), 20f,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                ((CircularGauge)bindable).Invalidate();
            });

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double MinValue
    {
        get => (double)GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    public double MaxValue
    {
        get => (double)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public Color ArcColor
    {
        get => (Color)GetValue(ArcColorProperty);
        set => SetValue(ArcColorProperty, value);
    }

    public Color BackgroundArcColor
    {
        get => (Color)GetValue(BackgroundArcColorProperty);
        set => SetValue(BackgroundArcColorProperty, value);
    }

    public float ArcThickness
    {
        get => (float)GetValue(ArcThicknessProperty);
        set => SetValue(ArcThicknessProperty, value);
    }

    public CircularGauge()
    {
        Drawable = new CircularGaugeDrawable(this);
    }

    private class CircularGaugeDrawable : IDrawable
    {
        private readonly CircularGauge _gauge;

        public CircularGaugeDrawable(CircularGauge gauge)
        {
            _gauge = gauge;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var centerX = dirtyRect.Center.X;
            var centerY = dirtyRect.Center.Y;
            var radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2 - _gauge.ArcThickness;

            // Screen coordinates: 0° = right(3:00), 90° = bottom(6:00), 180° = left(9:00), 270° = top(12:00)
            // Desired: 7:00 position = 120°, sweep CLOCKWISE through bottom to 5:00 = 60°
            // But we want to go the LONG way: 120° clockwise → 180° → 270° → 0° → 60° = 300° sweep

            const float startAngle = 120f;  // 7:00 position
            const float endAngle = 60f;     // 5:00 position (going clockwise = increasing in our method)

            // Draw background arc: clockwise from 120° to 60° (the long way = 300° sweep)
            var backgroundPath = new PathF();
            DrawArcPathClockwise(backgroundPath, centerX, centerY, radius, startAngle, endAngle, 300f);

            canvas.StrokeColor = _gauge.BackgroundArcColor;
            canvas.StrokeSize = _gauge.ArcThickness;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.DrawPath(backgroundPath);

            // Calculate progress
            var normalizedValue = (_gauge.Value - _gauge.MinValue) / (_gauge.MaxValue - _gauge.MinValue);
            normalizedValue = Math.Max(0, Math.Min(1, normalizedValue));

            // Progress sweep (0 to 300°)
            var progressSweep = (float)(normalizedValue * 300f);

            // Draw progress arc
            if (normalizedValue > 0)
            {
                var progressPath = new PathF();
                DrawArcPathClockwise(progressPath, centerX, centerY, radius, startAngle, 0f, progressSweep);

                canvas.StrokeColor = _gauge.ArcColor;
                canvas.StrokeSize = _gauge.ArcThickness;
                canvas.StrokeLineCap = LineCap.Round;
                canvas.DrawPath(progressPath);
            }
        }

        private void DrawArcPathClockwise(PathF path, float centerX, float centerY, float radius, float startAngle, float endAngle, float sweepDegrees)
        {
            // Draw arc clockwise by the specified sweep amount
            const int segments = 50;

            for (int i = 0; i <= segments; i++)
            {
                var t = (float)i / segments;
                var angle = startAngle + (t * sweepDegrees);

                // Convert to radians for screen coordinates (Y increases downward)
                var radians = (float)(angle * Math.PI / 180.0);

                var x = centerX + (float)(radius * Math.Cos(radians));
                var y = centerY + (float)(radius * Math.Sin(radians));

                if (i == 0)
                    path.MoveTo(x, y);
                else
                    path.LineTo(x, y);
            }
        }
    }
}
