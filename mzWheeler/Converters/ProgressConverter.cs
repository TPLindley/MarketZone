using System.Globalization;

namespace mzWheeler.Converters;

public class ProgressConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double doubleValue && parameter is string maxValueString)
        {
            if (double.TryParse(maxValueString, out double maxValue) && maxValue > 0)
            {
                return Math.Clamp(doubleValue / maxValue, 0.0, 1.0);
            }
        }
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
