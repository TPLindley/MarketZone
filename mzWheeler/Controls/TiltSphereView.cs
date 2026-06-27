using Microsoft.Maui.Controls;

namespace mzWheeler.Controls;

public class TiltSphereView : GraphicsView
{
    private readonly TiltSphere _sphere;

    public static readonly BindableProperty PitchProperty =
        BindableProperty.Create(nameof(Pitch), typeof(double), typeof(TiltSphereView), 0.0, propertyChanged: OnTiltChanged);

    public static readonly BindableProperty RollProperty =
        BindableProperty.Create(nameof(Roll), typeof(double), typeof(TiltSphereView), 0.0, propertyChanged: OnTiltChanged);

    public static readonly BindableProperty YawProperty =
        BindableProperty.Create(nameof(Yaw), typeof(double), typeof(TiltSphereView), 0.0, propertyChanged: OnTiltChanged);

    public double Pitch
    {
        get => (double)GetValue(PitchProperty);
        set => SetValue(PitchProperty, value);
    }

    public double Roll
    {
        get => (double)GetValue(RollProperty);
        set => SetValue(RollProperty, value);
    }

    public double Yaw
    {
        get => (double)GetValue(YawProperty);
        set => SetValue(YawProperty, value);
    }

    public TiltSphereView()
    {
        _sphere = new TiltSphere();
        Drawable = _sphere;
    }

    private static void OnTiltChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TiltSphereView view)
        {
            view._sphere.Pitch = view.Pitch;
            view._sphere.Roll = view.Roll;
            view._sphere.Yaw = view.Yaw;
            view.Invalidate();
        }
    }
}
