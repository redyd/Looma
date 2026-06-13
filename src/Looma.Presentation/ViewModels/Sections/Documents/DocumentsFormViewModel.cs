// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Repositories;
using Looma.Domain.Request;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.Services;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Documents;

public partial class DocumentsFormViewModel(
    INavigationService nav,
    IDocumentRepository repo,
    INotificationService notifications)
    : PageViewModelBase
{
    private Guid _editingId;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _nickname = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string? _sourcePath;

    [ObservableProperty] private string? _errorMessage;

    public string SelectedFileName =>
        string.IsNullOrWhiteSpace(SourcePath) ? "Aucun fichier sélectionné" : Path.GetFileName(SourcePath);

    public string SelectedFileDirectory =>
        string.IsNullOrWhiteSpace(SourcePath) ? string.Empty : Path.GetDirectoryName(SourcePath) ?? string.Empty;

    public void InitEdit(Guid id, string nickname)
    {
        _editingId = id;
        Title = "Modifier le document";
        Nickname = nickname;
        SourcePath = null;
        ErrorMessage = null;
    }

    partial void OnSourcePathChanged(string? value)
    {
        OnPropertyChanged(nameof(SelectedFileName));
        OnPropertyChanged(nameof(SelectedFileDirectory));
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Nickname);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        try
        {
            IsBusy = true;

            var result = await repo.UpdateAsync(new UpdateDocumentRequest(_editingId, Nickname));
            if (result.Failed)
            {
                ErrorMessage = result.Error;
                notifications.Error(result.Error ?? "Impossible de mettre à jour le document.");
                return;
            }

            notifications.Success("Le document a été mis à jour.");

            nav.GoBack();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => nav.GoBack();
}