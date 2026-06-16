// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Repositories;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Base;
using Looma.Domain.Core;
using Looma.Domain.Extensions;
using Looma.Domain.Request;
using Looma.Presentation.ViewModels.Shared;
using DocumentsPickerFormViewModel = Looma.Presentation.ViewModels.Shared.Documents.DocumentsPickerFormViewModel;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public partial class PatternsFormViewModel(
    INavigationService nav,
    IPatternRepository patternRepo,
    IDocumentRepository documentRepo,
    INotificationService notifications,
    DocumentsPickerFormViewModel documents)
    : PageViewModelBase
{
    private bool _isEdit;
    private int _patternId;

    public DocumentsPickerFormViewModel Documents => documents;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial bool DocumentsLoaded { get; set; } = true;

    [ObservableProperty]
    public partial string? Url { get; set; }

    [ObservableProperty]
    public partial string? Note { get; set; }

    [ObservableProperty]
    public partial PatternType Type { get; set; }

    [ObservableProperty]
    public partial bool IsPersonal { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? BeginDate { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? EndDate { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public IReadOnlyList<PatternType> PatternTypes { get; } = [.. Enum.GetValues<PatternType>()];
    public bool IsCreateMode => !_isEdit;
    public bool IsEditMode => _isEdit;
    private bool CanSave() => DocumentsLoaded && !string.IsNullOrWhiteSpace(Name);

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
                    _patternId,
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
                _patternId = savedPattern.Id;
                Title = "Modifier le patron";
                OnPropertyChanged(nameof(IsCreateMode));
                OnPropertyChanged(nameof(IsEditMode));
            }

            var hasDocumentChanges = documents.HasPendingChanges;
            if (!await documents.SaveAsync()) return;

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
        _patternId = 0;
        Title = "Nouveau patron";

        Name = string.Empty;
        Url = null;
        Note = null;
        Type = PatternType.Crochet;
        IsPersonal = false;
        BeginDate = null;
        EndDate = null;
        ErrorMessage = null;

        documents.InitCreate(async (sourcePath, nickname)
            => await documentRepo.AddAsync(new CreateDocumentRequest(sourcePath, nickname, PatternId: _patternId)));
        DocumentsLoaded = true;

        OnPropertyChanged(nameof(IsCreateMode));
        OnPropertyChanged(nameof(IsEditMode));
    }

    public async Task InitEdit(int id, string name, string? url, string? note, PatternType? type, bool? isPersonal,
        DateOnly? beginDate, DateOnly? endDate,
        IReadOnlyList<Guid> documentIds)
    {
        _isEdit = true;
        _patternId = id;
        Title = "Modifier le patron";

        Name = name;
        Url = url;
        Note = note;
        Type = type ?? PatternType.Crochet;
        IsPersonal = isPersonal ?? false;
        BeginDate = beginDate.ToDateTimeOffset();
        EndDate = endDate.ToDateTimeOffset();
        ErrorMessage = null;

        DocumentsLoaded = false;
        IsBusy = true;
        var ok = await documents.InitEditAsync(documentIds, async (id, nickname)
            => await documentRepo.UpdateAsync(new UpdateDocumentRequest(id, nickname)));

        if (!ok)
        {
            ErrorMessage = "Une erreur est survenue lors de la mise à jour des documents.";
        }

        IsBusy = false;
        DocumentsLoaded = true;

        OnPropertyChanged(nameof(IsCreateMode));
        OnPropertyChanged(nameof(IsEditMode));
    }

    private static string BuildSuccessMessage(bool wasEdit, bool hasDocumentChanges) =>
        hasDocumentChanges
            ? (wasEdit
                ? "Le patron et ses documents ont été mis à jour."
                : "Le patron et ses documents ont été ajoutés.")
            : (wasEdit ? "Le patron a été mis à jour." : "Le patron a été ajouté.");
}
