// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Services;
using Looma.Presentation.Notifications;
using Looma.Presentation.Services;
using Looma.Presentation.ViewModels.Base;
using Looma.Presentation.ViewModels.Shared.Settings;

namespace Looma.Presentation.ViewModels.Sections.Settings;

public partial class SettingsViewModel(
    ThemeService themeService,
    IThemeStorage themeStorage,
    IThemeFilePicker themeFilePicker,
    INotificationService notifications,
    TranslationService translation,
    SettingsUpdaterViewModel updater)
    : PageViewModelBase
{
    private bool _isLoadingThemes;
    private bool _isLoadingLanguages;

    public override bool KeepAliveInNavigationHistory => true;

    public ObservableCollection<ThemeOptionViewModel> Themes { get; } = [];
    public ObservableCollection<LanguageOptionViewModel> Languages { get; } = [];
    public SettingsUpdaterViewModel Updater { get; } = updater;

    [ObservableProperty]
    public partial ThemeOptionViewModel? SelectedTheme { get; set; }

    [ObservableProperty]
    public partial LanguageOptionViewModel? SelectedLanguage { get; set; }

    public override void OnNavigatedTo()
    {
        Title = "Paramètres";
        Updater.OnNavigatedTo();
        RefreshLanguages();
        RefreshThemes();
    }

    public override void OnNavigatedFrom()
    {
        base.OnNavigatedFrom();
        Updater.OnNavigatedFrom();
    }

    partial void OnSelectedThemeChanged(ThemeOptionViewModel? value)
    {
        OpenSelectedThemeCommand.NotifyCanExecuteChanged();
        DeleteSelectedThemeCommand.NotifyCanExecuteChanged();

        if (_isLoadingThemes || value is null)
            return;

        try
        {
            if (value.IsDefault)
            {
                themeService.ResetToDefault();
                themeStorage.SaveSelectedTheme(null);
                notifications.Success("Le thème par défaut a été appliqué.");
                return;
            }

            themeService.ApplyOverride(value.Path!);
            themeStorage.SaveSelectedTheme(value.Path);
            notifications.Success($"Le thème {value.Name} a été appliqué.");
        }
        catch (Exception ex)
        {
            notifications.Error($"Impossible d'appliquer le thème : {ex.Message}");
        }
    }

    partial void OnSelectedLanguageChanged(LanguageOptionViewModel? value)
    {
        if (_isLoadingLanguages || value is null)
            return;

        var previousCulture = CultureInfo.CurrentCulture.Name;
        var previousUiCulture = CultureInfo.CurrentUICulture.Name;

        try
        {
            translation.SetCulture(value.Culture);

            var currentCulture = CultureInfo.CurrentCulture.Name;
            var currentUiCulture = CultureInfo.CurrentUICulture.Name;

            Trace.TraceInformation(
                "Language changed from {0}/{1} to {2}/{3}.",
                previousCulture,
                previousUiCulture,
                currentCulture,
                currentUiCulture);

            if (!CultureMatches(value.Culture, CultureInfo.CurrentCulture)
                || !CultureMatches(value.Culture, CultureInfo.CurrentUICulture))
            {
                throw new InvalidOperationException(
                    $"La culture active est {currentCulture}/{currentUiCulture} au lieu de {value.Culture}.");
            }

            notifications.Success(translation["Success_SelectedLanguageChanged"]);
        }
        catch (Exception ex)
        {
            Trace.TraceError("Unable to change language to {0}: {1}", value.Culture, ex);
            notifications.Error($"Impossible de changer la langue : {ex.Message}");
            RefreshLanguages();
        }
    }

    private bool CanManageSelectedTheme() => SelectedTheme?.IsDefault == false;

    private static bool CultureMatches(string expectedCulture, CultureInfo actualCulture) =>
        string.Equals(actualCulture.Name, expectedCulture, StringComparison.OrdinalIgnoreCase)
        || string.Equals(actualCulture.TwoLetterISOLanguageName, expectedCulture, StringComparison.OrdinalIgnoreCase);

    private void RefreshLanguages()
    {
        _isLoadingLanguages = true;
        try
        {
            var selectedCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            Languages.Clear();
            foreach (var culture in TranslationService.SupportedLanguage)
                Languages.Add(new LanguageOptionViewModel(translation[culture], culture));

            SelectedLanguage = Languages.FirstOrDefault(language => language.Culture == selectedCulture)
                               ?? Languages.FirstOrDefault();
        }
        finally
        {
            _isLoadingLanguages = false;
        }
    }

    [RelayCommand]
    private void RefreshThemes()
    {
        var shouldReloadSelectedTheme = !_isLoadingThemes;
        _isLoadingThemes = true;
        try
        {
            var selectedPath = SelectedTheme?.Path;
            selectedPath ??= themeStorage.GetSelectedThemePath();

            Themes.Clear();
            Themes.Add(new ThemeOptionViewModel("Défaut", null));

            foreach (var file in themeStorage.GetThemeFiles())
            {
                var themeName = GetThemeDisplayName(file);
                Themes.Add(new ThemeOptionViewModel(themeName, file));
            }

            SelectedTheme = Themes.FirstOrDefault(theme => theme.Path == selectedPath)
                            ?? Themes.FirstOrDefault();

            if (!shouldReloadSelectedTheme || SelectedTheme is null)
                return;

            if (SelectedTheme.IsDefault)
            {
                themeService.ResetToDefault();
                themeStorage.SaveSelectedTheme(null);
            }
            else
            {
                themeService.ApplyOverride(SelectedTheme.Path!);
                themeStorage.SaveSelectedTheme(SelectedTheme.Path);
            }
        }
        catch (Exception ex)
        {
            notifications.Error($"Impossible de charger les thèmes : {ex.Message}");
        }
        finally
        {
            _isLoadingThemes = false;
        }
    }

    private string GetThemeDisplayName(string file)
    {
        try
        {
            return themeService.GetThemeName(file)
                   ?? System.IO.Path.GetFileNameWithoutExtension(file);
        }
        catch
        {
            return System.IO.Path.GetFileNameWithoutExtension(file);
        }
    }

    [RelayCommand]
    private async Task ImportThemeAsync()
    {
        var sourcePath = await themeFilePicker.PickThemeJsonAsync();
        if (string.IsNullOrWhiteSpace(sourcePath))
            return;

        try
        {
            var importedPath = themeStorage.ImportTheme(sourcePath);
            RefreshThemes();
            SelectedTheme = Themes.FirstOrDefault(theme => theme.Path == importedPath);
            notifications.Success("Le thème a été importé.");
        }
        catch (Exception ex)
        {
            notifications.Error($"Impossible d'importer le thème : {ex.Message}");
        }
    }

    [RelayCommand]
    private void ExportTheme()
    {
        try
        {
            var destinationPath = themeStorage.CreateExportPath();
            themeService.ExportCurrentOverride(destinationPath);
            notifications.Success($"Le thème a été exporté dans {destinationPath}.");
        }
        catch (Exception ex)
        {
            notifications.Error($"Impossible d'exporter le thème : {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanManageSelectedTheme))]
    private void OpenSelectedTheme()
    {
        try
        {
            var path = SelectedTheme?.Path;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                notifications.Error("Le fichier de thème est introuvable.");
                RefreshThemes();
                return;
            }

            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            notifications.Error($"Impossible d'ouvrir le thème : {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanManageSelectedTheme))]
    private void DeleteSelectedTheme()
    {
        try
        {
            var path = SelectedTheme?.Path;
            if (string.IsNullOrWhiteSpace(path))
                return;

            var deletedName = SelectedTheme?.Name ?? System.IO.Path.GetFileNameWithoutExtension(path);
            themeStorage.DeleteTheme(path);
            themeService.ResetToDefault();
            RefreshThemes();
            notifications.Success($"Le thème {deletedName} a été supprimé.");
        }
        catch (Exception ex)
        {
            notifications.Error($"Impossible de supprimer le thème : {ex.Message}");
        }
    }

}

public sealed record LanguageOptionViewModel(string Name, string Culture);
