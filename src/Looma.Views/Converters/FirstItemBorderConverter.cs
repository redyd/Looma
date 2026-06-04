using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Looma.Views.Converters;

public class FirstItemBorderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? new Thickness(0) : new Thickness(1, 0, 0, 0);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}