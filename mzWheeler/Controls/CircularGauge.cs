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

            // Draw background arc (260 degrees from 260 sweeping clockwise)
            canvas.StrokeColor = _gauge.BackgroundArcColor;
            canvas.StrokeSize = _gauge.ArcThickness;
            canvas.DrawArc(
                centerX - radius, centerY - radius,
                radius * 2, radius * 2,
                260, 260, true, false);

            // Calculate progress angle
            var normalizedValue = (_gauge.Value - _gauge.MinValue) / (_gauge.MaxValue - _gauge.MinValue);
            normalizedValue = Math.Max(0, Math.Min(1, normalizedValue)); // Clamp 0-1
            var progressAngle = normalizedValue * 260; // 260 degrees total range

            // Draw progress arc (clockwise from 260°)
            if (progressAngle > 0)
            {
                canvas.StrokeColor = _gauge.ArcColor;
                canvas.StrokeSize = _gauge.ArcThickness;
                canvas.DrawArc(
                    centerX - radius, centerY - radius,
                    radius * 2, radius * 2,
                    260, (float)progressAngle, true, false);
            }
        }
    }
}
