using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace mzWheeler.Controls;

public class CompassGaugeView : GraphicsView
{
    private readonly CompassGauge _gauge;

    public static readonly BindableProperty HeadingProperty =
        BindableProperty.Create(nameof(Heading), typeof(double), typeof(CompassGaugeView), 0.0, propertyChanged: OnHeadingChanged);

    public static readonly BindableProperty ArcColorProperty =
        BindableProperty.Create(nameof(ArcColor), typeof(Color), typeof(CompassGaugeView), Colors.Blue, propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty BackgroundArcColorProperty =
        BindableProperty.Create(nameof(BackgroundArcColor), typeof(Color), typeof(CompassGaugeView), Colors.Gray, propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty ArcThicknessProperty =
        BindableProperty.Create(nameof(ArcThickness), typeof(float), typeof(CompassGaugeView), 12f, propertyChanged: OnPropertyChanged);

    public double Heading
    {
        get => (double)GetValue(HeadingProperty);
        set => SetValue(HeadingProperty, value);
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

    public CompassGaugeView()
    {
        _gauge = new CompassGauge();
        Drawable = _gauge;
    }

    private static void OnHeadingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CompassGaugeView view)
        {
            view._gauge.Heading = view.Heading;
            view.Invalidate();
        }
    }

    private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CompassGaugeView view)
        {
            view._gauge.ArcColor = view.ArcColor;
            view._gauge.BackgroundArcColor = view.BackgroundArcColor;
            view._gauge.ArcThickness = view.ArcThickness;
            view.Invalidate();
        }
    }
}
