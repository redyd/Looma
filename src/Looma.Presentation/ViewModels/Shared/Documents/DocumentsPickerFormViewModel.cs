// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Extensions;
using Looma.Domain.IServices;
using Looma.Domain.Request;
using Looma.Presentation.Notifications;
using Looma.Presentation.Services;

namespace Looma.Presentation.ViewModels.Shared.Documents;

public partial class DocumentsPickerFormViewModel(
    IDocumentService documentService,
    IDocumentFilePicker filePicker,
    INotificationService notifications)
    : ObservableObject
{
    private readonly HashSet<Guid> _deletedDocumentIds = [];
    public int MaxDocuments { get; set; } = int.MaxValue;
    public DocumentPickerMode PickerMode { get; set; } = DocumentPickerMode.All;
    private Func<IReadOnlyList<CreateDocumentRequest>, Task<ResultBase>>? CreateDocumentsCallback { get; set; }
    private Func<Guid, string, Task<ResultBase>>? UpdateDocumentCallback { get; set; }


    [ObservableProperty]
    public partial ObservableCollection<DocumentFormSummaryViewModel> ExistingDocuments { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<DocumentDraftViewModel> NewDocuments { get; set; } = [];

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public bool CanAddDocument =>
        ExistingDocuments.Count + NewDocuments.Count(d => !string.IsNullOrWhiteSpace(d.SourcePath)) < MaxDocuments;

    public bool HasExistingDocuments => ExistingDocuments.Count > 0;
    public bool HasNewDocuments => NewDocuments.Any(d => !string.IsNullOrWhiteSpace(d.SourcePath));
    public bool HasDocuments => HasExistingDocuments || HasNewDocuments;

    public bool HasPendingChanges =>
        _deletedDocumentIds.Count > 0
        || ExistingDocuments.Any(d => d.Nickname != d.OriginalNickname)
        || NewDocuments.Any(d => !string.IsNullOrWhiteSpace(d.SourcePath));

    [RelayCommand(CanExecute = nameof(CanAddDocument))]
    private void AddDocument()
    {
        NewDocuments.Add(CreateDocumentDraft());
        RefreshDocumentState();
    }

    public async Task<bool> AddPickedDocumentsAsync(string? unsupportedFileMessage = null)
    {
        ErrorMessage = null;

        var paths = await filePicker.PicksAsync(PickerMode);
        if (paths.Count == 0)
            return true;

        if (paths.Any(path => !filePicker.IsSupportedPath(PickerMode, path)))
        {
            ErrorMessage = unsupportedFileMessage ?? TranslationService.Current["Documents_Notifications_UnableToAddDocument"];
            notifications.Error(ErrorMessage);
            return false;
        }

        foreach (var path in paths)
            AddDocumentDraft(path);

        RefreshDocumentState();
        return true;
    }

    public void InitCreate(Func<IReadOnlyList<CreateDocumentRequest>, Task<ResultBase>> createDocumentsCallback)
    {
        _deletedDocumentIds.Clear();
        ErrorMessage = null;
        ResetExistingDocuments();
        ResetNewDocuments();
        CreateDocumentsCallback = createDocumentsCallback;
        UpdateDocumentCallback = null;
    }

    public async Task<bool> InitEditAsync(
        IReadOnlyCollection<Guid> documentIds,
        Func<IReadOnlyList<CreateDocumentRequest>, Task<ResultBase>> createDocumentsCallback,
        Func<Guid, string, Task<ResultBase>> updateDocumentCallback)
    {
        _deletedDocumentIds.Clear();
        ErrorMessage = null;
        ResetExistingDocuments();
        ResetNewDocuments();
        CreateDocumentsCallback = createDocumentsCallback;
        UpdateDocumentCallback = updateDocumentCallback;

        if (documentIds.Count == 0)
            return true;

        var result = await documentService.GetAllAsync();
        if (result.Failed || result.Value is null)
        {
            notifications.Error(result.Error ?? TranslationService.Current["Documents_Notifications_UnableToLoadDocuments"]);
            return false;
        }

        var documents = result.Value
            .Where(d => documentIds.Contains(d.Id))
            .OrderBy(d => d.Nickname)
            .Select(d => new DocumentFormSummaryViewModel(
                d.Id,
                d.Nickname,
                d.Type,
                d.SizeBytes.ToBytesDisplay(),
                d.StoragePath,
                RemoveExistingDocument))
            .ToList();

        ExistingDocuments = new ObservableCollection<DocumentFormSummaryViewModel>(documents);
        RefreshDocumentState();
        return true;
    }

    public void InitEdit(
        IEnumerable<Document> documents,
        Func<IReadOnlyList<CreateDocumentRequest>, Task<ResultBase>> createDocumentsCallback,
        Func<Guid, string, Task<ResultBase>> updateDocumentCallback)
    {
        _deletedDocumentIds.Clear();
        ErrorMessage = null;
        ResetNewDocuments();
        CreateDocumentsCallback = createDocumentsCallback;
        UpdateDocumentCallback = updateDocumentCallback;

        ExistingDocuments = new ObservableCollection<DocumentFormSummaryViewModel>(
            documents
                .Where(document => filePicker.IsSupportedFile(PickerMode, document))
                .OrderBy(document => document.Nickname)
                .Select(document => new DocumentFormSummaryViewModel(
                    document.Id,
                    document.Nickname,
                    document.Type,
                    document.SizeBytes.ToBytesDisplay(),
                    document.StoragePath,
                    RemoveExistingDocument)));

        RefreshDocumentState();
    }

    /// <summary>
    /// Syncs deletions/renames of existing documents and uploads new drafts for the given entity.
    /// Returns false and notifies on first error.
    /// </summary>
    public async Task<bool> SaveAsync()
    {
        foreach (var deletedId in _deletedDocumentIds.ToList())
        {
            var result = await documentService.DeleteAsync(deletedId);
            if (result.Failed)
            {
                notifications.Error(result.Error ?? TranslationService.Current["Documents_Notifications_UnableToDeleteDocument"]);
                return false;
            }
        }

        foreach (var document in ExistingDocuments)
        {
            if (_deletedDocumentIds.Contains(document.DocumentId) || document.Nickname == document.OriginalNickname)
                continue;

            if (UpdateDocumentCallback is null)
            {
                notifications.Error(TranslationService.Current["Documents_Notifications_UnableToRenameDocumentHere"]);
                return false;
            }

            var result = await UpdateDocumentCallback(document.DocumentId, document.Nickname);
            if (result.Failed)
            {
                notifications.Error(result.Error ?? TranslationService.Current["Documents_Notifications_UnableToRenameDocument"]);
                return false;
            }
        }

        var newDocumentRequests = new List<CreateDocumentRequest>();
        var newDocumentDrafts = NewDocuments
            .Where(d => !string.IsNullOrWhiteSpace(d.SourcePath))
            .ToList();

        foreach (var draft in newDocumentDrafts)
        {
            if (string.IsNullOrWhiteSpace(draft.Nickname))
                draft.Nickname = Path.GetFileNameWithoutExtension(draft.SourcePath!);

            if (!filePicker.IsSupportedPath(PickerMode, draft.SourcePath))
            {
                ErrorMessage = TranslationService.Current["Documents_Notifications_UnableToAddDocument"];
                notifications.Error(ErrorMessage);
                return false;
            }

            newDocumentRequests.Add(new CreateDocumentRequest(draft.SourcePath!, draft.Nickname));
        }

        if (newDocumentRequests.Count > 0)
        {
            if (CreateDocumentsCallback is null)
            {
                notifications.Error(TranslationService.Current["Documents_Notifications_UnableToAddDocumentHere"]);
                return false;
            }

            var result = await CreateDocumentsCallback(newDocumentRequests);
            if (result.Failed)
            {
                notifications.Error(result.Error ?? TranslationService.Current["Documents_Notifications_UnableToAddDocument"]);
                return false;
            }

            foreach (var draft in newDocumentDrafts)
            {
                draft.Reset();
            }
        }

        return true;
    }

    private DocumentDraftViewModel CreateDocumentDraft() => new(filePicker, PickerMode, RemoveDocument);

    private void AddDocumentDraft(string path)
    {
        var draft = NewDocuments.FirstOrDefault(document => string.IsNullOrWhiteSpace(document.SourcePath));
        if (draft is null)
        {
            draft = CreateDocumentDraft();
            NewDocuments.Add(draft);
        }

        draft.SourcePath = path;
        if (string.IsNullOrWhiteSpace(draft.Nickname))
            draft.Nickname = Path.GetFileNameWithoutExtension(path);
    }

    private void ResetNewDocuments() => NewDocuments = [CreateDocumentDraft()];

    private void ResetExistingDocuments() => ExistingDocuments = [];

    private void RemoveDocument(DocumentDraftViewModel draft)
    {
        if (NewDocuments.Count <= 1)
        {
            draft.Reset();
            RefreshDocumentState();
            return;
        }

        NewDocuments.Remove(draft);
        RefreshDocumentState();
    }

    private void RemoveExistingDocument(DocumentFormSummaryViewModel document)
    {
        _deletedDocumentIds.Add(document.DocumentId);
        ExistingDocuments.Remove(document);
        RefreshDocumentState();
    }

    partial void OnExistingDocumentsChanged(ObservableCollection<DocumentFormSummaryViewModel> value) =>
        RefreshDocumentState();

    partial void OnNewDocumentsChanged(ObservableCollection<DocumentDraftViewModel> value) =>
        RefreshDocumentState();

    private void RefreshDocumentState()
    {
        OnPropertyChanged(nameof(CanAddDocument));
        OnPropertyChanged(nameof(HasExistingDocuments));
        OnPropertyChanged(nameof(HasNewDocuments));
        OnPropertyChanged(nameof(HasDocuments));
        OnPropertyChanged(nameof(HasPendingChanges));
        AddDocumentCommand.NotifyCanExecuteChanged();
    }
}
