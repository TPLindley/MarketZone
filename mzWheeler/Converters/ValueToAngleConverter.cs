using System.Globalization;

namespace mzWheeler.Converters;

/// <summary>
/// Converts a value to a rotation angle for circular gauge needles
/// Value range maps to angle range (typically 0-270 degrees for a 3/4 circle gauge)
/// </summary>
public class ValueToAngleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double doubleValue && parameter is string paramString)
        {
            // Parameter format: "maxValue,startAngle,endAngle"
            // Example: "200,135,405" means 0-200 maps to 135°-405° (3/4 circle starting bottom-left)
            var parts = paramString.Split(',');
            if (parts.Length == 3 &&
                double.TryParse(parts[0], out double maxValue) &&
                double.TryParse(parts[1], out double startAngle) &&
                double.TryParse(parts[2], out double endAngle))
            {
                // Clamp value to 0-maxValue
                doubleValue = Math.Clamp(doubleValue, 0, maxValue);

                // Map value to angle range
                var progress = doubleValue / maxValue;
                var angleRange = endAngle - startAngle;
                var angle = startAngle + (progress * angleRange);

                return angle;
            }
        }
        return 135.0; // Default starting angle
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
