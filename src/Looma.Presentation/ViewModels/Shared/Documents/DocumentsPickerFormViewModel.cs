// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
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

    public bool CanAddDocument =>
        ExistingDocuments.Count + NewDocuments.Count(d => !string.IsNullOrWhiteSpace(d.SourcePath)) < MaxDocuments;

    public bool HasPendingChanges =>
        _deletedDocumentIds.Count > 0
        || ExistingDocuments.Any(d => d.Nickname != d.OriginalNickname)
        || NewDocuments.Any(d => !string.IsNullOrWhiteSpace(d.SourcePath));

    [RelayCommand(CanExecute = nameof(CanAddDocument))]
    private void AddDocument() => NewDocuments.Add(CreateDocumentDraft());

    public void InitCreate(Func<IReadOnlyList<CreateDocumentRequest>, Task<ResultBase>> createDocumentsCallback)
    {
        _deletedDocumentIds.Clear();
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
        ResetExistingDocuments();
        ResetNewDocuments();
        CreateDocumentsCallback = createDocumentsCallback;
        UpdateDocumentCallback = updateDocumentCallback;

        if (documentIds.Count == 0)
            return true;

        var result = await documentService.GetAllAsync();
        if (result.Failed || result.Value is null)
        {
            notifications.Error(result.Error ?? "Impossible de charger les documents.");
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
                RemoveExistingDocument))
            .ToList();

        ExistingDocuments = new ObservableCollection<DocumentFormSummaryViewModel>(documents);
        return true;
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
                notifications.Error(result.Error ?? "Impossible de supprimer le document.");
                return false;
            }
        }

        foreach (var document in ExistingDocuments)
        {
            if (_deletedDocumentIds.Contains(document.DocumentId) || document.Nickname == document.OriginalNickname)
                continue;

            if (UpdateDocumentCallback is null)
            {
                notifications.Error("Impossible de renommer le document dans ce contexte.");
                return false;
            }

            var result = await UpdateDocumentCallback(document.DocumentId, document.Nickname);
            if (result.Failed)
            {
                notifications.Error(result.Error ?? "Impossible de renommer le document.");
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

            newDocumentRequests.Add(new CreateDocumentRequest(draft.SourcePath!, draft.Nickname));
        }

        if (newDocumentRequests.Count > 0)
        {
            if (CreateDocumentsCallback is null)
            {
                notifications.Error("Impossible d'ajouter le document dans ce contexte.");
                return false;
            }

            var result = await CreateDocumentsCallback(newDocumentRequests);
            if (result.Failed)
            {
                notifications.Error(result.Error ?? "Impossible d'ajouter le document.");
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

    private void ResetNewDocuments() => NewDocuments = [CreateDocumentDraft()];

    private void ResetExistingDocuments() => ExistingDocuments = [];

    private void RemoveDocument(DocumentDraftViewModel draft)
    {
        if (NewDocuments.Count <= 1)
        {
            draft.Reset();
            return;
        }

        NewDocuments.Remove(draft);
        OnPropertyChanged(nameof(CanAddDocument));
        AddDocumentCommand.NotifyCanExecuteChanged();
    }

    private void RemoveExistingDocument(DocumentFormSummaryViewModel document)
    {
        _deletedDocumentIds.Add(document.DocumentId);
        ExistingDocuments.Remove(document);
        OnPropertyChanged(nameof(CanAddDocument));
        AddDocumentCommand.NotifyCanExecuteChanged();
    }
}
