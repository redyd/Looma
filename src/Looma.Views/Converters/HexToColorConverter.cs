// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

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
            try
            {
                return new SolidColorBrush(Color.Parse(hex));
            }
            catch
            {
                return new SolidColorBrush(Colors.Gray);
            }
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}