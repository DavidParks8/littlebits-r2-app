using System.Globalization;

namespace Game2048.Maui.Converters;

public class TileValueToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int tileValue || tileValue == 0)
            return string.Empty;

        return tileValue.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
