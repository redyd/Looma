// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.IServices;
using Looma.Domain.Request;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Documents;

public partial class DocumentsFormViewModel(
    INavigationService nav,
    IDocumentService documentService,
    INotificationService notifications)
    : PageViewModelBase
{
    private Guid _editingId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Nickname { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string? SourcePath { get; set; }

    public void InitEdit(Guid id, string nickname)
    {
        _editingId = id;
        Title = Translation["DocumentsForm_EditTitle"];
        Nickname = nickname;
        SourcePath = null;
    }

    public string SelectedFileName =>
        string.IsNullOrWhiteSpace(SourcePath) ? Translation["Documents_NoFileSelected"] : Path.GetFileName(SourcePath);

    public string SelectedFileDirectory =>
        string.IsNullOrWhiteSpace(SourcePath) ? string.Empty : Path.GetDirectoryName(SourcePath) ?? string.Empty;

    private bool CanSave() => !string.IsNullOrWhiteSpace(Nickname);

    partial void OnSourcePathChanged(string? value)
    {
        OnPropertyChanged(nameof(SelectedFileName));
        OnPropertyChanged(nameof(SelectedFileDirectory));
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;

            var result = await documentService.UpdateAsync(new UpdateDocumentRequest(_editingId, Nickname));
            if (result.Failed)
            {
                notifications.Error(result.Error ?? Translation["Documents_Notifications_UnableToUpdateDocument"]);
                return;
            }

            notifications.Success(Translation["Documents_Notifications_DocumentUpdated"]);

            nav.GoBack();
        }
        catch (Exception ex)
        {
            notifications.Error(Translation.Format("Common_UnexpectedErrorWithMessage", ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => nav.GoBack();
}
