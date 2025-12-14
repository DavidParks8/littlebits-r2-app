using System.Globalization;

namespace Game2048.Maui.Converters;

public class TileValueToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int tileValue)
            return Colors.Transparent;

        return tileValue switch
        {
            0 => Color.FromArgb("#CDC1B4"),      // Empty
            2 => Color.FromArgb("#EEE4DA"),      // Tile2
            4 => Color.FromArgb("#EDE0C8"),      // Tile4
            8 => Color.FromArgb("#F2B179"),      // Tile8
            16 => Color.FromArgb("#F59563"),     // Tile16
            32 => Color.FromArgb("#F67C5F"),     // Tile32
            64 => Color.FromArgb("#F65E3B"),     // Tile64
            128 => Color.FromArgb("#EDCF72"),    // Tile128
            256 => Color.FromArgb("#EDCC61"),    // Tile256
            512 => Color.FromArgb("#EDC850"),    // Tile512
            1024 => Color.FromArgb("#EDC53F"),   // Tile1024
            2048 => Color.FromArgb("#EDC22E"),   // Tile2048
            _ => Color.FromArgb("#3C3A32")       // TileSuper (>2048)
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
