// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Extensions;
using Looma.Domain.Repositories;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.UserControls;
using Looma.Presentation.ViewModels.Base;
using Looma.Presentation.ViewModels.Sections.Documents;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public partial class PatternsDetailViewModel(
    INavigationService nav,
    IPatternRepository patternRepo,
    IDocumentRepository documentRepo,
    INotificationService notifications) : PageViewModelBase
{
    private int _patternId;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Url { get; set; }

    [ObservableProperty]
    public partial string? Note { get; set; }

    [ObservableProperty]
    public partial PatternType Type { get; set; }

    [ObservableProperty]
    public partial bool IsPersonal { get; set; }

    [ObservableProperty]
    public partial DateOnly? BeginDate { get; set; }

    [ObservableProperty]
    public partial DateOnly? EndDate { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DocumentSummaryViewModel> Documents { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<PatternProject> Projects { get; set; } = [];

    public bool HasUrl => !string.IsNullOrWhiteSpace(Url);
    public bool HasDocuments => Documents.Count > 0;
    public bool HasProjects => Projects.Count > 0;
    public string NoteDisplay => string.IsNullOrWhiteSpace(Note) ? "Aucune note." : Note!;

    public IList<StatItem> DetailStats =>
    [
        new() { Label = "Nombre de projets lié", Value = Projects.Count.ToString("N0"), Unit = "x", IsFirst = true },
    ];

    public IList<InfoItem> DetailInfos =>
    [
        new() { Label = "Nom", Value = Name },
        new() { Label = "Lien", Value = Url ?? "Aucun" },
        new() { Label = "Type", Value = Type.GetDisplayName() },
        new() { Label = "Patron", Value = IsPersonal ? "Personnel" : "Non personnel" },
        new() { Label = "Début", Value = BeginDate.FormatWithDefault("Aucune") },
        new() { Label = "Fin", Value = EndDate.FormatWithDefault("Aucune") },
        new() { Label = "Documents", Value = Documents.Count.ToString("N0") },
    ];

    public void Load(Pattern pattern)
    {
        Title = "Détail patron";
        _patternId = pattern.Id;

        ApplyPattern(pattern);
    }

    public override async void OnNavigatedTo()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (_patternId == 0) return;

        var pattern = await patternRepo.GetByIdAsync(_patternId);
        if (pattern.Failed || pattern.Value is null)
        {
            ErrorMessage = pattern.Error ?? $"Le patron {_patternId} est introuvable.";
            return;
        }

        ApplyPattern(pattern.Value);
    }

    private void ApplyPattern(Pattern pattern)
    {
        ErrorMessage = null;
        Name = pattern.Name;
        Url = pattern.Url;
        Note = pattern.Note;
        Type = pattern.Type;
        IsPersonal = pattern.IsPersonal;
        BeginDate = pattern.BeginDate;
        EndDate = pattern.EndDate;

        Documents = new ObservableCollection<DocumentSummaryViewModel>(
            pattern.Documents.Select(d => new DocumentSummaryViewModel(
                d,
                new AsyncRelayCommand(() => OpenDocumentAsync(d.Id)))));

        Projects = new ObservableCollection<PatternProject>(pattern.Projects);

        OnPropertyChanged(nameof(HasUrl));
        OnPropertyChanged(nameof(HasDocuments));
        OnPropertyChanged(nameof(HasProjects));
        OnPropertyChanged(nameof(NoteDisplay));
        OnPropertyChanged(nameof(DetailInfos));
    }

    private async Task OpenDocumentAsync(Guid id)
    {
        var result = await documentRepo.OpenAsync(id);
        if (result.Failed)
            notifications.Error(result.Error ?? "Impossible d'ouvrir le document.");
    }

    [RelayCommand]
    private async Task OpenUrlAsync()
    {
        if (string.IsNullOrWhiteSpace(Url))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(Url)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            notifications.Error($"Impossible d'ouvrir le lien: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Edit() =>
        nav.NavigateTo<PatternsFormViewModel>(vm =>
            vm.InitEdit(_patternId, Name, Url, Note, Type, IsPersonal, BeginDate, EndDate,
                Documents.Select(d => d.Document.Id).ToList()));

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        IsBusy = true;
        try
        {
            var result = await patternRepo.DeleteAsync(_patternId);
            if (result.Failed)
            {
                ErrorMessage = result.Error;
                notifications.Error(result.Error ?? "Impossible de supprimer le patron.");
                return;
            }

            notifications.Success("Le patron a été supprimé.");
            nav.GoBack();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoBack() => nav.GoBack();
}
