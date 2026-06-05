using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.Services;
using Looma.Presentation.UserControls;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public partial class PatternsDetailViewModel : PageViewModelBase
{
    private readonly INavigationService _nav;
    private readonly IPatternRepository _patternRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly INotificationService _notifications;
    private readonly IDataRefreshService _refresh;

    [ObservableProperty] private int _patternId;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _url;
    [ObservableProperty] private string? _note;
    [ObservableProperty] private DateOnly? _beginDate;
    [ObservableProperty] private DateOnly? _endDate;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private ObservableCollection<PatternDocumentViewModel> _documents = [];
    [ObservableProperty] private ObservableCollection<PatternProjectViewModel> _projects = [];

    public bool HasUrl => !string.IsNullOrWhiteSpace(Url);
    public bool HasDocuments => Documents.Count > 0;
    public bool HasProjects => Projects.Count > 0;
    public string NoteDisplay => string.IsNullOrWhiteSpace(Note) ? "Aucune note." : Note!;

    public IList<StatItem> DetailStats =>
    [
        // new() { Label = "Pelottes estimée", Value = "-1", Unit = "x"},
        new() { Label = "Nombre de projets lié", Value = Projects.Count.ToString("N0"), Unit = "x", IsFirst = true },
    ];

    public IList<InfoItem> DetailInfos =>
    [
        new() { Label = "Nom", Value = Name },
        new() { Label = "Lien", Value = Url ?? "Aucun" },
        new() { Label = "Début", Value = FormatDate(BeginDate) },
        new() { Label = "Fin", Value = FormatDate(EndDate) },
        new() { Label = "Documents", Value = Documents.Count.ToString("N0") },
    ];

    public PatternsDetailViewModel(
        INavigationService nav,
        IPatternRepository patternRepo,
        IDocumentRepository documentRepo,
        INotificationService notifications,
        IDataRefreshService refresh)
    {
        _nav = nav;
        _patternRepo = patternRepo;
        _documentRepo = documentRepo;
        _notifications = notifications;
        _refresh = refresh;
        Title = "Détail patron";
        _refresh.PatternsRefreshRequested += OnPatternsRefreshRequested;
    }

    public void Load(Pattern pattern)
    {
        PatternId = pattern.Id;
        ApplyPattern(pattern);
    }

    public override async void OnNavigatedTo()
    {
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (PatternId == 0)
            return;

        var pattern = await _patternRepo.GetByIdAsync(PatternId);
        if (pattern.Failed || pattern.Value is null)
        {
            ErrorMessage = pattern.Error ?? $"Le patron {PatternId} est introuvable.";
            return;
        }

        ApplyPattern(pattern.Value);
    }

    private void OnPatternsRefreshRequested(object? sender, EventArgs e) => _ = RefreshAsync();

    private void ApplyPattern(Pattern pattern)
    {
        ErrorMessage = null;
        Name = pattern.Name;
        Url = pattern.Url;
        Note = pattern.Note;
        BeginDate = pattern.BeginDate;
        EndDate = pattern.EndDate;
        
        Documents = new ObservableCollection<PatternDocumentViewModel>(
            pattern.Documents.Select(d => new PatternDocumentViewModel(
                d,
                new AsyncRelayCommand(() => OpenDocumentAsync(d.Id)))));

        Projects = new ObservableCollection<PatternProjectViewModel>(
            pattern.Projects.Select(p => new PatternProjectViewModel(p)));

        OnPropertyChanged(nameof(HasUrl));
        OnPropertyChanged(nameof(HasDocuments));
        OnPropertyChanged(nameof(HasProjects));
        OnPropertyChanged(nameof(NoteDisplay));
        OnPropertyChanged(nameof(DetailInfos));
    }

    private static string FormatDate(DateOnly? value) =>
        value is null ? "Aucune" : value.Value.ToString("dd/MM/yyyy");

    private async Task OpenDocumentAsync(Guid id)
    {
        var result = await _documentRepo.OpenAsync(id);
        if (result.Failed)
            _notifications.Error(result.Error ?? "Impossible d'ouvrir le document.");
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
            _notifications.Error($"Impossible d'ouvrir le lien: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Edit() =>
        _nav.NavigateTo<PatternsFormViewModel>(vm =>
            vm.InitEdit(PatternId, Name, Url, Note, BeginDate, EndDate, Documents.Select(d => d.Document.Id).ToList()));

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _patternRepo.DeleteAsync(PatternId);
            if (result.Failed)
            {
                ErrorMessage = result.Error;
                _notifications.Error(result.Error ?? "Impossible de supprimer le patron.");
                return;
            }

            _notifications.Success("Le patron a été supprimé.");
            _nav.GoBack();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoBack() => _nav.GoBack();
}
