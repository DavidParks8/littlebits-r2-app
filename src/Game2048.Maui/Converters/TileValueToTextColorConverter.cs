using System.Globalization;

namespace Game2048.Maui.Converters;

public class TileValueToTextColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int tileValue)
            return Colors.Black;

        // Use dark text for lighter tiles (2, 4)
        // Use light text for darker tiles (8+)
        return tileValue <= 4
            ? Color.FromArgb("#776E65")  // TileTextLight
            : Color.FromArgb("#F9F6F2");  // TileTextDark
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
