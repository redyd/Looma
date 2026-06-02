using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Base;
using System.Collections.ObjectModel;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public partial class PatternsFormViewModel(
    INavigationService nav,
    IDocumentRepository documentRepo,
    IPatternRepository repo,
    INotificationService notifications)
    : PageViewModelBase
{
    private bool _isEdit;
    private int _editingId;
    private IReadOnlyList<Guid> _selectedDocumentIds = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _url;

    [ObservableProperty]
    private string? _note;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private ObservableCollection<PatternDocumentSelectionViewModel> _documents = [];

    public bool IsCreateMode => !_isEdit;
    public bool IsEditMode => _isEdit;

    public void InitCreate()
    {
        _isEdit = false;
        _editingId = 0;
        Title = "Nouveau patron";
        Name = string.Empty;
        Url = null;
        Note = null;
        ErrorMessage = null;
        Documents = [];
        _selectedDocumentIds = [];
        OnPropertyChanged(nameof(IsCreateMode));
        OnPropertyChanged(nameof(IsEditMode));
        _ = LoadDocumentsAsync(Array.Empty<Guid>());
    }

    public void InitEdit(int id, string name, string? url, string? note, IReadOnlyList<Guid> documentIds)
    {
        _isEdit = true;
        _editingId = id;
        Title = "Modifier le patron";
        Name = name;
        Url = url;
        Note = note;
        ErrorMessage = null;
        _selectedDocumentIds = documentIds.ToList();
        OnPropertyChanged(nameof(IsCreateMode));
        OnPropertyChanged(nameof(IsEditMode));
        _ = LoadDocumentsAsync(documentIds);
    }

    private async Task LoadDocumentsAsync(IReadOnlyCollection<Guid> selectedIds)
    {
        var result = await documentRepo.GetAllAsync();
        if (result.Failed || result.Value is null)
        {
            Documents = [];
            return;
        }

        Documents = new ObservableCollection<PatternDocumentSelectionViewModel>(
            result.Value
                .OrderBy(d => d.Nickname)
                .Select(d => new PatternDocumentSelectionViewModel(d, selectedIds.Contains(d.Id))));
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

    private IReadOnlyList<Guid> GetSelectedDocumentIds() =>
        Documents
            .Where(d => d.IsSelected)
            .Select(d => d.Document.Id)
            .ToList();

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        try
        {
            IsBusy = true;

            if (_isEdit)
            {
                var result = await repo.UpdateAsync(new UpdatePatternRequest(
                    _editingId,
                    Name,
                    Url,
                    Note,
                    GetSelectedDocumentIds()));
                if (result.Failed)
                {
                    ErrorMessage = result.Error;
                    notifications.Error(result.Error ?? "Impossible de mettre à jour le patron.");
                    return;
                }

                notifications.Success("Le patron a été mis à jour.");
            }
            else
            {
                var result = await repo.AddAsync(new CreatePatternRequest(
                    Name,
                    Url,
                    Note,
                    GetSelectedDocumentIds()));
                if (result.Failed)
                {
                    ErrorMessage = result.Error;
                    notifications.Error(result.Error ?? "Impossible d'ajouter le patron.");
                    return;
                }

                notifications.Success("Le patron a été ajouté.");
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
