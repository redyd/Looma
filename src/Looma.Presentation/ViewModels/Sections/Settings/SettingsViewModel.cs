// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Logging;
using Looma.Domain.IServices;
using Looma.Domain.Services;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.Services;
using Looma.Presentation.ViewModels.Base;
using Looma.Presentation.ViewModels.Shared.Settings;

namespace Looma.Presentation.ViewModels.Sections.Settings;

public partial class SettingsViewModel(
    INavigationService nav,
    ThemeService themeService,
    IThemeStorage themeStorage,
    IThemeFilePicker themeFilePicker,
    ISettingsService settingsService,
    INotificationService notifications,
    TranslationService translation,
    SettingsUpdaterViewModel updater,
    IDomainLogger logger)
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
        Title = translation["Settings_Title"];
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
                notifications.Success(translation["Settings_Notifications_DefaultThemeApplied"]);
                return;
            }

            themeService.ApplyOverride(value.Path!);
            themeStorage.SaveSelectedTheme(value.Path);
            notifications.Success(translation.Format("Settings_Notifications_ThemeApplied", value.Name));
        }
        catch (Exception ex)
        {
            notifications.Error(translation.Format("Settings_Notifications_UnableToApplyTheme", ex.Message));
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
            var saveResult = settingsService.SetSelectedLanguageAsync(value.Culture).GetAwaiter().GetResult();
            if (!saveResult.Succeeded)
                throw new InvalidOperationException(saveResult.Error);

            var currentCulture = CultureInfo.CurrentCulture.Name;
            var currentUiCulture = CultureInfo.CurrentUICulture.Name;

            logger.Log(
                DomainLogLevel.Information,
                $"Language changed from {previousCulture}/{previousUiCulture} to {currentCulture}/{currentUiCulture}.");

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
            logger.Log(DomainLogLevel.Error, $"Unable to change language to {value.Culture}.", ex);
            notifications.Error(translation.Format("Settings_Notifications_UnableToChangeLanguage", ex.Message));
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
            Themes.Add(new ThemeOptionViewModel(translation["Settings_DefaultTheme"], null));

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
            notifications.Error(translation.Format("Settings_Notifications_UnableToLoadThemes", ex.Message));
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
                   ?? Path.GetFileNameWithoutExtension(file);
        }
        catch
        {
            return Path.GetFileNameWithoutExtension(file);
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
            notifications.Success(translation["Settings_Notifications_ThemeImported"]);
        }
        catch (Exception ex)
        {
            notifications.Error(translation.Format("Settings_Notifications_UnableToImportTheme", ex.Message));
        }
    }

    [RelayCommand]
    private void ExportTheme()
    {
        try
        {
            var destinationPath = themeStorage.CreateExportPath();
            themeService.ExportCurrentOverride(destinationPath);
            notifications.Success(translation.Format("Settings_Notifications_ThemeExported", destinationPath));
        }
        catch (Exception ex)
        {
            notifications.Error(translation.Format("Settings_Notifications_UnableToExportTheme", ex.Message));
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
                notifications.Error(translation["Settings_Notifications_ThemeFileNotFound"]);
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
            notifications.Error(translation.Format("Settings_Notifications_UnableToOpenTheme", ex.Message));
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

            var deletedName = SelectedTheme?.Name ?? Path.GetFileNameWithoutExtension(path);
            themeStorage.DeleteTheme(path);
            themeService.ResetToDefault();
            RefreshThemes();
            notifications.Success(translation.Format("Settings_Notifications_ThemeDeleted", deletedName));
        }
        catch (Exception ex)
        {
            notifications.Error(translation.Format("Settings_Notifications_UnableToDeleteTheme", ex.Message));
        }
    }

    [RelayCommand]
    private void OpenReport() => nav.NavigateTo<ReportViewModel>();

    [RelayCommand]
    private void SupportAuthor()
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://www.buymeacoffee.com/redyd")
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            notifications.Error(translation.Format("Settings_Notifications_UnableToOpenSupportLink", ex.Message));
        }
    }
}


public sealed record LanguageOptionViewModel(string Name, string Culture);
