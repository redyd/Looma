// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Domain.Localization;

/// <summary>
/// Ambient translation dictionary for domain and infrastructure messages.
/// Set once at startup; until then keys are returned as-is.
/// </summary>
public static class Localizer
{
    public static ILocalizer Current { get; set; } = new KeyLocalizer();

    public static string Get(string key) => Current[key];

    public static string Format(string key, params object[] args) => Current.Format(key, args);

    private sealed class KeyLocalizer : ILocalizer
    {
        public string this[string key] => key;

        public string Format(string key, params object[] args) =>
            args.Length == 0 ? key : $"{key}: {string.Join(", ", args)}";
    }
}
