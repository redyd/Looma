// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Looma.Domain.Extensions;

public static class EnumExtension
{
    public static string GetDisplayName(this Enum value) =>
        value.GetType()
            .GetField(value.ToString())!
            .GetCustomAttribute<DisplayAttribute>()?.Name ?? value.ToString();
}