// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.Services;
using Looma.Presentation.ViewModels.Base;
using System.Collections.ObjectModel;
using Looma.Domain.Core;
using Looma.Domain.Extensions;
using Looma.Domain.Request;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public partial class PatternsFormViewModel(
    INavigationService nav,
    IPatternRepository patternRepo,
    IDocumentRepository documentRepo,
    IDocumentFilePicker filePicker,
    IDataRefreshService refresh,
    INotificationService notifications)
    : PageViewModelBase
{
    private readonly HashSet<Guid> _deletedDocumentIds = [];
    private bool _isEdit;
    private int _editingId;

    public bool IsCreateMode => !_isEdit;
    public bool IsEditMode => _isEdit;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _documentsLoaded = true;

    public IReadOnlyList<PatternType> PatternTypes { get; } = Enum.GetValues<PatternType>().ToList();

    [ObservableProperty] private string? _url;
    [ObservableProperty] private string? _note;
    [ObservableProperty] private PatternType _type;
    [ObservableProperty] private bool _isPersonal;
    [ObservableProperty] private DateTimeOffset? _beginDate;
    [ObservableProperty] private DateTimeOffset? _endDate;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private ObservableCollection<PatternExistingDocumentViewModel> _existingDocuments = [];
    [ObservableProperty] private ObservableCollection<PatternDocumentDraftViewModel> _newDocuments = [];

    [RelayCommand]
    private void AddDocument() => NewDocuments.Add(CreateDocumentDraft());

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        try
        {
            IsBusy = true;
            var wasEdit = _isEdit;

            var patternResult = _isEdit
                ? await patternRepo.UpdateAsync(new UpdatePatternRequest(
                    _editingId,
                    Name,
                    Url,
                    Note,
                    Type,
                    IsPersonal,
                    BeginDate.ToDateOnly(),
                    EndDate.ToDateOnly()))
                : await patternRepo.AddAsync(new CreatePatternRequest(
                    Name,
                    Url,
                    Note,
                    Type,
                    IsPersonal,
                    BeginDate.ToDateOnly(),
                    EndDate.ToDateOnly()));

            if (patternResult.Failed || patternResult.Value is null)
            {
                ErrorMessage = patternResult.Error;
                notifications.Error(patternResult.Error ?? "Impossible de sauvegarder le patron.");
                return;
            }

            var savedPattern = patternResult.Value;
            if (!wasEdit)
            {
                _isEdit = true;
                _editingId = savedPattern.Id;
                Title = "Modifier le patron";
                OnPropertyChanged(nameof(IsCreateMode));
                OnPropertyChanged(nameof(IsEditMode));
            }

            var hasDocumentChanges = CountPendingDocumentChanges() > 0;
            if (!await SyncExistingDocumentsAsync())
                return;

            if (!await CreateNewDocumentsAsync(savedPattern.Id))
                return;

            if (hasDocumentChanges)
            {
                refresh.RequestDocumentsRefresh();
                refresh.RequestPatternsRefresh();
            }

            notifications.Success(BuildSuccessMessage(wasEdit, hasDocumentChanges));
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

    public void InitCreate()
    {
        _isEdit = false;
        _editingId = 0;
        Title = "Nouveau patron";

        Name = string.Empty;
        Url = null;
        Note = null;
        Type = PatternType.Crochet;
        IsPersonal = false;
        BeginDate = null;
        EndDate = null;
        ErrorMessage = null;

        _deletedDocumentIds.Clear();
        ResetExistingDocuments();
        ResetNewDocuments();
        DocumentsLoaded = true;
        OnPropertyChanged(nameof(IsCreateMode));
        OnPropertyChanged(nameof(IsEditMode));
    }

    public void InitEdit(int id, string name, string? url, string? note, PatternType? type, bool? isPersonal,
        DateOnly? beginDate, DateOnly? endDate,
        IReadOnlyList<Guid> documentIds)
    {
        _isEdit = true;
        _editingId = id;
        Title = "Modifier le patron";

        Name = name;
        Url = url;
        Note = note;
        Type = type ?? PatternType.Crochet;
        IsPersonal = isPersonal ?? false;
        BeginDate = beginDate.ToDateTimeOffset();
        EndDate = endDate.ToDateTimeOffset();
        ErrorMessage = null;

        _deletedDocumentIds.Clear();
        ResetExistingDocuments();
        ResetNewDocuments();
        DocumentsLoaded = false;
        IsBusy = true;
        OnPropertyChanged(nameof(IsCreateMode));
        OnPropertyChanged(nameof(IsEditMode));
        _ = LoadExistingDocumentsAsync(documentIds);
    }

    private bool CanSave() => DocumentsLoaded && !string.IsNullOrWhiteSpace(Name);

    private PatternDocumentDraftViewModel CreateDocumentDraft() =>
        new(filePicker, RemoveDocument);

    private void ResetNewDocuments() =>
        NewDocuments = new ObservableCollection<PatternDocumentDraftViewModel>
        {
            CreateDocumentDraft()
        };

    private void ResetExistingDocuments() =>
        ExistingDocuments = [];

    private async Task LoadExistingDocumentsAsync(IReadOnlyCollection<Guid> selectedIds)
    {
        if (!_isEdit || selectedIds.Count == 0)
        {
            IsBusy = false;
            DocumentsLoaded = true;
            return;
        }

        var result = await documentRepo.GetAllAsync();
        if (result.Failed || result.Value is null)
        {
            ErrorMessage = result.Error;
            notifications.Error(result.Error ?? "Impossible de charger les documents du patron.");
            IsBusy = false;
            DocumentsLoaded = true;
            return;
        }

        var documents = result.Value
            .Where(d => selectedIds.Contains(d.Id))
            .OrderBy(d => d.Nickname)
            .Select(d => new PatternExistingDocumentViewModel(
                d.Id,
                d.Nickname,
                d.Type,
                FormatSize(d.SizeBytes),
                RemoveExistingDocument))
            .ToList();

        ExistingDocuments = new ObservableCollection<PatternExistingDocumentViewModel>(documents);
        IsBusy = false;
        DocumentsLoaded = true;
    }

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0)
            return "0 B";

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var size = (double)bytes;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{bytes} {units[unitIndex]}"
            : $"{size:0.##} {units[unitIndex]}";
    }

    private void RemoveDocument(PatternDocumentDraftViewModel draft)
    {
        if (NewDocuments.Count <= 1)
        {
            draft.Reset();
            return;
        }

        NewDocuments.Remove(draft);
    }

    private void RemoveExistingDocument(PatternExistingDocumentViewModel document)
    {
        _deletedDocumentIds.Add(document.DocumentId);
        ExistingDocuments.Remove(document);
    }

    private int CountPendingDocumentChanges() =>
        _deletedDocumentIds.Count
        + ExistingDocuments.Count(document => document.Nickname != document.OriginalNickname)
        + NewDocuments.Count(document => !string.IsNullOrWhiteSpace(document.SourcePath));

    private static string BuildSuccessMessage(bool wasEdit, bool hasDocumentChanges) =>
        hasDocumentChanges
            ? (wasEdit
                ? "Le patron et ses documents ont été mis à jour."
                : "Le patron et ses documents ont été ajoutés.")
            : (wasEdit ? "Le patron a été mis à jour." : "Le patron a été ajouté.");

    private async Task<bool> SyncExistingDocumentsAsync()
    {
        foreach (var deletedId in _deletedDocumentIds.ToList())
        {
            var deleteResult = await documentRepo.DeleteAsync(deletedId);
            if (deleteResult.Failed)
            {
                ErrorMessage = deleteResult.Error;
                notifications.Error(deleteResult.Error ?? "Impossible de supprimer le document.");
                return false;
            }
        }

        foreach (var document in ExistingDocuments)
        {
            if (_deletedDocumentIds.Contains(document.DocumentId))
                continue;

            if (document.Nickname == document.OriginalNickname)
                continue;

            var updateResult = await documentRepo.UpdateAsync(new UpdateDocumentRequest(
                document.DocumentId,
                document.Nickname));

            if (updateResult.Failed)
            {
                ErrorMessage = updateResult.Error;
                notifications.Error(updateResult.Error ?? "Impossible de renommer le document.");
                return false;
            }
        }

        return true;
    }

    private async Task<bool> CreateNewDocumentsAsync(int patternId)
    {
        foreach (var draft in NewDocuments.Where(d => !string.IsNullOrWhiteSpace(d.SourcePath)))
        {
            if (string.IsNullOrWhiteSpace(draft.Nickname) && !string.IsNullOrWhiteSpace(draft.SourcePath))
                draft.Nickname = Path.GetFileNameWithoutExtension(draft.SourcePath);

            var documentResult = await documentRepo.AddAsync(new CreateDocumentRequest(
                draft.SourcePath!,
                draft.Nickname,
                patternId));

            if (documentResult.Failed)
            {
                ErrorMessage = documentResult.Error;
                notifications.Error(documentResult.Error ?? "Impossible d'ajouter le document au patron.");
                return false;
            }

            draft.Reset();
        }

        return true;
    }
}