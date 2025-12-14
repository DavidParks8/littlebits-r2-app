using System.Globalization;

namespace Game2048.Maui.Converters;

public class TileValueToColorConverter : IValueConverter
{
    private static readonly Dictionary<int, Color> _colorMap = new()
    {
        { 0, Color.FromArgb("#CDC1B4") },      // Empty
        { 2, Color.FromArgb("#EEE4DA") },      // Tile2
        { 4, Color.FromArgb("#EDE0C8") },      // Tile4
        { 8, Color.FromArgb("#F2B179") },      // Tile8
        { 16, Color.FromArgb("#F59563") },     // Tile16
        { 32, Color.FromArgb("#F67C5F") },     // Tile32
        { 64, Color.FromArgb("#F65E3B") },     // Tile64
        { 128, Color.FromArgb("#EDCF72") },    // Tile128
        { 256, Color.FromArgb("#EDCC61") },    // Tile256
        { 512, Color.FromArgb("#EDC850") },    // Tile512
        { 1024, Color.FromArgb("#EDC53F") },   // Tile1024
        { 2048, Color.FromArgb("#EDC22E") },   // Tile2048
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int tileValue)
            return Colors.Transparent;

        // Return mapped color if it exists
        if (_colorMap.TryGetValue(tileValue, out var color))
            return color;

        // For values > 2048, generate a color based on the tile value
        // Use a darker shade that gets progressively darker with higher values
        var baseColor = Color.FromArgb("#3C3A32");
        
        // Calculate how many doublings beyond 2048 (e.g., 4096 = 1, 8192 = 2, etc.)
        int doublings = 0;
        int temp = tileValue;
        while (temp > 2048)
        {
            temp /= 2;
            doublings++;
        }

        // Create progressively darker colors for higher tile values
        // Every 3 doublings, make it slightly lighter again to maintain visibility
        float brightness = 1.0f - (doublings % 3) * 0.1f;
        
        return Color.FromRgba(
            (int)(0x3C * brightness),
            (int)(0x3A * brightness),
            (int)(0x32 * brightness),
            255
        );
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
