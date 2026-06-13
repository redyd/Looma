// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Looma.Views.Converters;

public class StringToUriConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s)) return null;
        try
        {
            if (File.Exists(s))
                return new Bitmap(s);

            if (!Uri.TryCreate(s, UriKind.Absolute, out var uri))
                return null;

            if (uri.IsFile && File.Exists(uri.LocalPath))
                return new Bitmap(uri.LocalPath);

            if (uri.Scheme != "avares")
                return null;

            using var stream = AssetLoader.Open(uri);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
