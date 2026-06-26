// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Avalonia.Data.Converters;
using Looma.Domain.Extensions;
using Looma.Presentation.Services;

namespace Looma.Views.Converters;

public class EnumDisplayNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Enum enumValue) return value;

        var translated = TranslationService.Current[$"Enum_{enumValue}"];
        return translated.StartsWith('!') ? enumValue.GetDisplayName() : translated;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
