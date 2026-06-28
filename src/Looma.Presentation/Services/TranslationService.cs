// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Looma.Presentation.Services;

public sealed class TranslationService : INotifyPropertyChanged
{
    public static readonly string[] SupportedLanguage = ["fr", "en", "nl", "de", "es"];
    public static TranslationService Current { get; private set; } = new();

    private readonly ResourceManager _resourceManager =
        new("Looma.Presentation.Resources.Translations", typeof(TranslationService).Assembly);

    public TranslationService()
    {
        Current = this;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] =>
        _resourceManager.GetString(key, CultureInfo.CurrentUICulture)
        ?? $"!{key}!";

    public string Format(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentUICulture, this[key], args);

    public void SetCulture(string culture)
    {
        var info = new CultureInfo(culture);

        CultureInfo.CurrentCulture = info;
        CultureInfo.CurrentUICulture = info;
        CultureInfo.DefaultThreadCurrentCulture = info;
        CultureInfo.DefaultThreadCurrentUICulture = info;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
        });
    }
}
