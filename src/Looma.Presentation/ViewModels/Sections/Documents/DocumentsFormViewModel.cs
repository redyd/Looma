using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.Services;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Documents;

public partial class DocumentsFormViewModel(
    INavigationService nav,
    IDocumentRepository repo,
    IDocumentFilePicker filePicker,
    INotificationService notifications)
    : PageViewModelBase
{
    private bool _isEdit;
    private Guid _editingId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _nickname = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string? _sourcePath;

    [ObservableProperty]
    private string? _errorMessage;

    public string SelectedFileName =>
        string.IsNullOrWhiteSpace(SourcePath) ? "Aucun fichier sélectionné" : Path.GetFileName(SourcePath);

    public string SelectedFileDirectory =>
        string.IsNullOrWhiteSpace(SourcePath) ? string.Empty : Path.GetDirectoryName(SourcePath) ?? string.Empty;

    public bool IsCreateMode => !_isEdit;
    public bool IsEditMode => _isEdit;

    public void InitCreate()
    {
        _isEdit = false;
        _editingId = Guid.Empty;
        Title = "Nouveau document";
        Nickname = string.Empty;
        SourcePath = null;
        ErrorMessage = null;
        OnPropertyChanged(nameof(IsCreateMode));
        OnPropertyChanged(nameof(IsEditMode));
    }

    public void InitEdit(Guid id, string nickname)
    {
        _isEdit = true;
        _editingId = id;
        Title = "Modifier le document";
        Nickname = nickname;
        SourcePath = null;
        ErrorMessage = null;
        OnPropertyChanged(nameof(IsCreateMode));
        OnPropertyChanged(nameof(IsEditMode));
    }

    partial void OnSourcePathChanged(string? value)
    {
        OnPropertyChanged(nameof(SelectedFileName));
        OnPropertyChanged(nameof(SelectedFileDirectory));
    }

    private bool CanSave() =>
        _isEdit
            ? !string.IsNullOrWhiteSpace(Nickname)
            : !string.IsNullOrWhiteSpace(SourcePath);

    [RelayCommand]
    private async Task BrowseFileAsync()
    {
        if (_isEdit)
            return;

        var path = await filePicker.PickDocumentAsync();
        if (string.IsNullOrWhiteSpace(path))
            return;

        SourcePath = path;
        if (string.IsNullOrWhiteSpace(Nickname))
            Nickname = Path.GetFileNameWithoutExtension(path);
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        try
        {
            IsBusy = true;

            if (_isEdit)
            {
                var result = await repo.UpdateAsync(new UpdateDocumentRequest(_editingId, Nickname));
                if (result.Failed)
                {
                    ErrorMessage = result.Error;
                    notifications.Error(result.Error ?? "Impossible de mettre à jour le document.");
                    return;
                }

                notifications.Success("Le document a été mis à jour.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(SourcePath))
                {
                    ErrorMessage = "Veuillez sélectionner un fichier.";
                    return;
                }

                var result = await repo.AddAsync(new CreateDocumentRequest(SourcePath, Nickname));
                if (result.Failed)
                {
                    ErrorMessage = result.Error;
                    notifications.Error(result.Error ?? "Impossible d'ajouter le document.");
                    return;
                }

                notifications.Success("Le document a été ajouté.");
            }

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
