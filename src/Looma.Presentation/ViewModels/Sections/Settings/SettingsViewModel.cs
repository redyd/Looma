// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Services;
using Looma.Presentation.Notifications;
using Looma.Presentation.Services;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Settings;

public partial class SettingsViewModel(
    ThemeService themeService,
    IThemeStorage themeStorage,
    IThemeFilePicker themeFilePicker,
    INotificationService notifications)
    : PageViewModelBase
{
    private bool _isLoadingThemes;

    public ObservableCollection<ThemeOptionViewModel> Themes { get; } = [];

    [ObservableProperty]
    public partial ThemeOptionViewModel? SelectedTheme { get; set; }

    public override void OnNavigatedTo()
    {
        Title = "Paramètres";
        RefreshThemes();
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

    private bool CanManageSelectedTheme() => SelectedTheme?.IsDefault == false;

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
