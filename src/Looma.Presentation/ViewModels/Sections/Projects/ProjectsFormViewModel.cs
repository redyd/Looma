// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Extensions;
using Looma.Domain.Request;
using Looma.Domain.Search;
using Looma.Domain.Services;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.Services;
using Looma.Presentation.ViewModels.Base;
using Looma.Presentation.ViewModels.Shared;
using Looma.Presentation.ViewModels.Shared.Projects;

namespace Looma.Presentation.ViewModels.Sections.Projects;

public partial class ProjectsFormViewModel(
    INavigationService nav,
    IProjectService projectService,
    IPatternService patternService,
    IWoolService woolService,
    IDocumentService documentService,
    IDocumentFilePicker filePicker,
    INotificationService notifications,
    PatternSearchSpec patternSearchSpec,
    WoolSearchSpec woolSearchSpec)
    : PageViewModelBase
{
    private bool _isEdit;
    private int _editingId;
    private IReadOnlyList<Pattern> _allPatterns = [];
    private IReadOnlyList<Wool> _allWools = [];
    private readonly HashSet<int> _selectedWoolIds = [];
    private readonly HashSet<Guid> _deletedImageIds = [];

    private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp",
        ".bmp",
        ".gif"
    };

    public IReadOnlyList<Status> Statuses { get; } = Enum.GetValues<Status>().ToList();

    public IReadOnlyList<ProjectPatternTypeFilterViewModel> PatternTypeFilters { get; } =
    [
        new("Tous les types", null),
        ..Enum.GetValues<PatternType>()
            .Select(type => new ProjectPatternTypeFilterViewModel(type.GetDisplayName(), type))
    ];

    public bool HasSelectedPattern => SelectedPattern is not null;
    public bool HasSelectedWools => SelectedWools.Count > 0;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasExistingImages => ExistingImages.Count > 0;
    public bool HasNewImages => NewImages.Count > 0;
    public bool HasImages => HasExistingImages || HasNewImages;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty] public partial Status Status { get; set; } = Status.InProgress;

    [ObservableProperty] public partial string? Note { get; set; }

    [ObservableProperty] public partial DateTimeOffset? BeginDate { get; set; }

    [ObservableProperty] public partial DateTimeOffset? EndDate { get; set; }

    [ObservableProperty] public partial string PatternSearchQuery { get; set; } = string.Empty;

    [ObservableProperty] public partial string WoolSearchQuery { get; set; } = string.Empty;

    [ObservableProperty] public partial ProjectPatternTypeFilterViewModel? SelectedPatternTypeFilter { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial Pattern? SelectedPattern { get; set; }

    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<ProjectSelectablePatternViewModel> SelectedPatterns { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ProjectSelectablePatternViewModel> PatternResults { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ProjectSelectableWoolViewModel> WoolResults { get; set; } = [];

    [ObservableProperty] private ObservableCollection<ProjectSelectableWoolViewModel> _selectedWools = [];
    [ObservableProperty] private ObservableCollection<ProjectImageViewModel> _existingImages = [];
    [ObservableProperty] private ObservableCollection<ProjectImageDraftViewModel> _newImages = [];

    public void InitCreate()
    {
        _isEdit = false;
        _editingId = 0;
        Title = "Nouveau projet";
        Name = string.Empty;
        Status = Status.InProgress;
        Note = null;
        BeginDate = DateTimeOffset.Now;
        EndDate = null;
        SelectedPattern = null;
        PatternSearchQuery = string.Empty;
        SelectedPatternTypeFilter = PatternTypeFilters[0];
        WoolSearchQuery = string.Empty;
        ErrorMessage = null;
        _selectedWoolIds.Clear();
        _deletedImageIds.Clear();
        ExistingImages = [];
        NewImages = [];
        _ = LoadChoicesAsync();
    }

    public void InitEdit(Project project)
    {
        _isEdit = true;
        _editingId = project.ProjectId;
        Title = "Modifier le projet";
        Name = project.Name;
        Status = project.Status;
        Note = project.Note;
        BeginDate = project.BeginDate.ToDateTimeOffset();
        EndDate = project.EndDate.ToDateTimeOffset();
        SelectedPattern = project.Pattern;
        PatternSearchQuery = string.Empty;
        SelectedPatternTypeFilter = PatternTypeFilters[0];
        WoolSearchQuery = string.Empty;
        ErrorMessage = null;
        _selectedWoolIds.Clear();
        _deletedImageIds.Clear();
        foreach (var woolId in project.Wools.Select(w => w.Wool.Id))
            _selectedWoolIds.Add(woolId);
        ExistingImages = new ObservableCollection<ProjectImageViewModel>(
            project.Files
                .Where(IsSupportedImage)
                .Select(image => new ProjectImageViewModel(
                    image,
                    new RelayCommand(() => RemoveExistingImage(image.Id)))));
        NewImages = [];
        _ = LoadChoicesAsync();
    }

    private async Task LoadChoicesAsync()
    {
        IsBusy = true;
        try
        {
            var patternsResult = await patternService.GetAllAsync();
            var woolsResult = await woolService.GetAllAsync();

            if (patternsResult.Failed || patternsResult.Value is null)
            {
                ErrorMessage = patternsResult.Error;
                notifications.Error(patternsResult.Error ?? "Impossible de charger les patrons.");
                return;
            }

            if (woolsResult.Failed || woolsResult.Value is null)
            {
                ErrorMessage = woolsResult.Error;
                notifications.Error(woolsResult.Error ?? "Impossible de charger les laines.");
                return;
            }

            _allPatterns = patternsResult.Value;
            _allWools = woolsResult.Value;
            SelectedPattern = SelectedPattern is null
                ? null
                : _allPatterns.FirstOrDefault(p => p.Id == SelectedPattern.Id) ?? SelectedPattern;
            ApplyPatternSearch();
            ApplyWoolSearch();
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnNameChanged(string value) => SaveCommand.NotifyCanExecuteChanged();
    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    partial void OnSelectedPatternChanged(Pattern? value)
    {
        OnPropertyChanged(nameof(HasSelectedPattern));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedWoolsChanged(ObservableCollection<ProjectSelectableWoolViewModel> value) =>
        OnPropertyChanged(nameof(HasSelectedWools));

    partial void OnExistingImagesChanged(ObservableCollection<ProjectImageViewModel> value) =>
        NotifyImagesChanged();

    partial void OnNewImagesChanged(ObservableCollection<ProjectImageDraftViewModel> value) =>
        NotifyImagesChanged();

    partial void OnPatternSearchQueryChanged(string value) => ApplyPatternSearch();
    partial void OnWoolSearchQueryChanged(string value) => ApplyWoolSearch();
    partial void OnSelectedPatternTypeFilterChanged(ProjectPatternTypeFilterViewModel? value) => ApplyPatternSearch();

    private void ApplyPatternSearch()
    {
        var query = PatternSearchQuery;
        var patterns = patternSearchSpec.Apply(_allPatterns, query)
            .Where(p => SelectedPatternTypeFilter?.Type is null || p.Type == SelectedPatternTypeFilter.Type)
            .Where(p => SelectedPattern?.Id != p.Id)
            .OrderBy(p => p.Name)
            .Take(10)
            .ToList();

        PatternResults = new ObservableCollection<ProjectSelectablePatternViewModel>(
            patterns.Select(pattern => new ProjectSelectablePatternViewModel(
                pattern,
                false,
                new RelayCommand(() => SelectPattern(pattern)))));

        var selectedPattern = SelectedPattern;
        SelectedPatterns = new ObservableCollection<ProjectSelectablePatternViewModel>(
            selectedPattern is null
                ? []
                :
                [
                    new ProjectSelectablePatternViewModel(
                        selectedPattern,
                        true,
                        new RelayCommand(() => SelectPattern(selectedPattern)))
                ]);
    }

    private void ApplyWoolSearch()
    {
        var wools = woolSearchSpec.Apply(_allWools, WoolSearchQuery)
            .Where(w => !_selectedWoolIds.Contains(w.Id))
            .OrderBy(w => w.Brand)
            .ThenBy(w => w.Name)
            .Take(12)
            .ToList();

        var selectedWools = _allWools
            .Where(w => _selectedWoolIds.Contains(w.Id))
            .OrderBy(w => w.Brand)
            .ThenBy(w => w.Name)
            .ToList();

        WoolResults = new ObservableCollection<ProjectSelectableWoolViewModel>(
            wools.Select(BuildWoolChoice));
        SelectedWools = new ObservableCollection<ProjectSelectableWoolViewModel>(
            selectedWools.Select(BuildWoolChoice));
    }

    private ProjectSelectableWoolViewModel BuildWoolChoice(Wool wool) =>
        new(wool, _selectedWoolIds.Contains(wool.Id), new RelayCommand(() => ToggleWool(wool)));

    private void SelectPattern(Pattern pattern)
    {
        SelectedPattern = pattern;
        ApplyPatternSearch();
    }

    [RelayCommand]
    private void ClearPattern()
    {
        SelectedPattern = null;
        ApplyPatternSearch();
    }

    private void ToggleWool(Wool wool)
    {
        if (!_selectedWoolIds.Add(wool.Id))
            _selectedWoolIds.Remove(wool.Id);

        ApplyWoolSearch();
        OnPropertyChanged(nameof(HasSelectedWools));
    }

    private bool CanSave() =>
        !string.IsNullOrWhiteSpace(Name);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var woolIds = _selectedWoolIds.ToList();
            var result = _isEdit
                ? await projectService.UpdateAsync(new UpdateProjectRequest(
                    _editingId,
                    Name,
                    Status,
                    Note,
                    BeginDate.ToDateOnly(),
                    EndDate.ToDateOnly(),
                    SelectedPattern?.Id,
                    woolIds))
                : await projectService.AddAsync(new CreateProjectRequest(
                    Name,
                    Status,
                    Note,
                    BeginDate.ToDateOnly(),
                    EndDate.ToDateOnly(),
                    SelectedPattern?.Id,
                    woolIds));

            if (result.Failed || result.Value is null)
            {
                ErrorMessage = result.Error;
                notifications.Error(result.Error ?? "Impossible de sauvegarder le projet.");
                return;
            }

            if (!await SyncImagesAsync(result.Value.ProjectId))
                return;

            notifications.Success(_isEdit ? "Le projet a été mis à jour." : "Le projet a été créé.");
            nav.GoBack();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BrowseImagesAsync()
    {
        var paths = await filePicker.PicksAsync(DocumentPickerMode.Images);
        if (paths.Count == 0)
            return;

        var invalid = paths.Where(path => !IsSupportedImagePath(path)).ToList();
        if (invalid.Count > 0)
        {
            ErrorMessage = "Seuls les image sont acceptés.";
            notifications.Error(ErrorMessage);
            return;
        }

        foreach (var path in paths)
            NewImages.Add(new ProjectImageDraftViewModel(path, RemoveNewImage));

        NotifyImagesChanged();
    }

    private void RemoveExistingImage(Guid imageId)
    {
        _deletedImageIds.Add(imageId);
        ExistingImages = new ObservableCollection<ProjectImageViewModel>(
            ExistingImages.Where(image => image.Document.Id != imageId));
        NotifyImagesChanged();
    }

    private void RemoveNewImage(Shared.Projects.ProjectImageDraftViewModel image)
    {
        NewImages.Remove(image);
        NotifyImagesChanged();
    }

    private async Task<bool> SyncImagesAsync(int projectId)
    {
        foreach (var imageId in _deletedImageIds.ToList())
        {
            var deleteResult = await documentService.DeleteAsync(imageId);
            if (deleteResult.Failed)
            {
                ErrorMessage = deleteResult.Error;
                notifications.Error(deleteResult.Error ?? "Impossible de supprimer l'image.");
                return false;
            }
        }

        var newImageRequests = new List<CreateDocumentRequest>();
        foreach (var image in NewImages.ToList())
        {
            if (!IsSupportedImagePath(image.SourcePath))
            {
                ErrorMessage = "Seuls les fichiers image PNG, JPG, WEBP, BMP ou GIF sont acceptés.";
                notifications.Error(ErrorMessage);
                return false;
            }

            newImageRequests.Add(new CreateDocumentRequest(
                image.SourcePath,
                string.IsNullOrWhiteSpace(image.Nickname)
                    ? Path.GetFileNameWithoutExtension(image.SourcePath)
                    : image.Nickname,
                ProjectId: projectId));
        }

        if (newImageRequests.Count > 0)
        {
            var documentResult = await documentService.AddAllAsync(newImageRequests);
            if (documentResult.Failed)
            {
                ErrorMessage = documentResult.Error;
                notifications.Error(documentResult.Error ?? "Impossible d'ajouter les images au projet.");
                return false;
            }
        }

        return true;
    }

    private static bool IsSupportedImage(Document document) =>
        document.StoragePath is not null && IsSupportedImagePath(document.StoragePath);

    private static bool IsSupportedImagePath(string path) =>
        SupportedImageExtensions.Contains(Path.GetExtension(path));

    private void NotifyImagesChanged()
    {
        OnPropertyChanged(nameof(HasExistingImages));
        OnPropertyChanged(nameof(HasNewImages));
        OnPropertyChanged(nameof(HasImages));
    }

    [RelayCommand]
    private void Cancel() => nav.GoBack();
}
