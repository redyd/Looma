using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace Looma.Views.Converters;

public class HexToColorConverter : IValueConverter
{
    public static readonly HexToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex)
        {
            try { return new SolidColorBrush(Color.Parse(hex)); }
            catch { return new SolidColorBrush(Colors.Gray); }
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}