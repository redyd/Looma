// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.IServices;
using Looma.Domain.Refresh;
using Looma.Domain.Request;
using Looma.Domain.Services;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.Services;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.Tests.TestSupport;

internal sealed class FakeNavigationService : INavigationService
{
    public PageViewModelBase? CurrentPage { get; private set; }
    public bool CanGoBack { get; set; }
    public int GoBackCount { get; private set; }
    public int ClearHistoryCount { get; private set; }
    public List<Type> NavigatedTypes { get; } = [];
    public List<PageViewModelBase> PushedPages { get; } = [];
    public List<Action<object>> ConfigureCalls { get; } = [];
    public Func<Type, PageViewModelBase>? ViewModelFactory { get; set; }

    public event EventHandler<PageViewModelBase>? Navigated;

    public void NavigateTo<TViewModel>(Action<TViewModel>? configure = null)
        where TViewModel : PageViewModelBase
    {
        NavigatedTypes.Add(typeof(TViewModel));

        if (ViewModelFactory?.Invoke(typeof(TViewModel)) is TViewModel vm)
        {
            configure?.Invoke(vm);
            CurrentPage = vm;
            Navigated?.Invoke(this, vm);
            return;
        }

        if (configure is not null)
            ConfigureCalls.Add(target => configure((TViewModel)target));
    }

    public void PushPage(PageViewModelBase page)
    {
        PushedPages.Add(page);
        CurrentPage = page;
        Navigated?.Invoke(this, page);
    }

    public void GoBack() => GoBackCount++;
    public void ClearHistory() => ClearHistoryCount++;
}

internal sealed class FakeNotificationService : INotificationService
{
    private readonly List<NotificationItemViewModel> _notifications = [];
    public IReadOnlyList<NotificationItemViewModel> Notifications => _notifications;
    public List<(NotificationSeverity Severity, string Message, string? Title)> Calls { get; } = [];

    public NotificationItemViewModel Info(string message, string? title = null, TimeSpan? duration = null) =>
        Add(NotificationSeverity.Info, message, title, duration);

    public NotificationItemViewModel Success(string message, string? title = null, TimeSpan? duration = null) =>
        Add(NotificationSeverity.Success, message, title, duration);

    public NotificationItemViewModel Warning(string message, string? title = null, TimeSpan? duration = null) =>
        Add(NotificationSeverity.Warning, message, title, duration);

    public NotificationItemViewModel Error(string message, string? title = null, TimeSpan? duration = null) =>
        Add(NotificationSeverity.Error, message, title, duration);

    public void Dismiss(Guid id) => _notifications.RemoveAll(n => n.Id == id);
    public void Clear() => _notifications.Clear();

    private NotificationItemViewModel Add(NotificationSeverity severity, string message, string? title, TimeSpan? duration)
    {
        Calls.Add((severity, message, title));
        var item = new NotificationItemViewModel(Guid.NewGuid(), severity, title ?? severity.ToString(), message, _ => { });
        _notifications.Add(item);
        return item;
    }
}

internal sealed class FakeRefreshService : IDataRefreshService
{
    public event EventHandler<DataRefreshRequestedEventArgs>? RefreshRequested;
    public int SubscriberCount => RefreshRequested?.GetInvocationList().Length ?? 0;
    public void RequestRefresh(RefreshScope scope, string reason) =>
        RefreshRequested?.Invoke(this, new DataRefreshRequestedEventArgs(scope, reason));
}

internal sealed class FakeUpdateInteractionService : IUpdateInteractionService
{
    public event EventHandler? UpdatePromptRequested;
    public event EventHandler? CurrentReleaseNotesRequested;

    public int UpdatePromptRequestCount { get; private set; }
    public int CurrentReleaseNotesRequestCount { get; private set; }

    public void RequestUpdatePrompt()
    {
        UpdatePromptRequestCount++;
        UpdatePromptRequested?.Invoke(this, EventArgs.Empty);
    }

    public void RequestCurrentReleaseNotes()
    {
        CurrentReleaseNotesRequestCount++;
        CurrentReleaseNotesRequested?.Invoke(this, EventArgs.Empty);
    }
}

internal sealed class FakeUpdaterService : IUpdaterService
{
    public event EventHandler? StateChanged;

    public UpdateStatus Status { get; set; } = UpdateStatus.Idle;
    public UpdateChannel Channel { get; set; } = UpdateChannel.Stable;
    public string CurrentVersion { get; set; } = "1.0.0";
    public string CurrentReleaseNotes { get; set; } = string.Empty;
    public int DownloadProgress { get; set; }
    public string? ErrorMessage { get; set; }
    public UpdateInformations? UpdateInformations { get; set; }

    public int CheckCalls { get; private set; }
    public int UpdateCalls { get; private set; }
    public int MarkShownCalls { get; private set; }
    public bool ShouldShowReleaseNotes { get; set; }
    public Action<FakeUpdaterService, bool>? OnCheck { get; set; }
    public Action<FakeUpdaterService>? OnUpdate { get; set; }

    public Task CheckForUpdatesAsync(bool silent = false)
    {
        CheckCalls++;
        OnCheck?.Invoke(this, silent);
        StateChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(IProgress<int>? progress = null)
    {
        UpdateCalls++;
        OnUpdate?.Invoke(this);
        StateChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task<bool> ShouldShowCurrentReleaseNotesAsync() => Task.FromResult(ShouldShowReleaseNotes);

    public Task MarkCurrentReleaseNotesAsShownAsync()
    {
        MarkShownCalls++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeThemeStorage : IThemeStorage
{
    public IReadOnlyList<string> ThemeFiles { get; set; } = [];
    public string? SelectedThemePath { get; set; }

    public IReadOnlyList<string> GetThemeFiles() => ThemeFiles;
    public string? GetSelectedThemePath() => SelectedThemePath;
    public int SeedThemeFiles(string sourceFolder) => 0;
    public void SaveSelectedTheme(string? themePath) => SelectedThemePath = themePath;
    public void DeleteTheme(string themePath) { }
    public string ImportTheme(string sourcePath) => sourcePath;
    public string CreateExportPath() => Path.Combine(Path.GetTempPath(), "theme.json");
}

internal sealed class FakeThemeFilePicker : IThemeFilePicker
{
    public string? NextPick { get; set; }

    public Task<string?> PickThemeJsonAsync() => Task.FromResult(NextPick);
}

internal sealed class FakeDocumentFilePicker : IDocumentFilePicker
{
    public string? NextPick { get; set; }
    public List<string> NextPicks { get; set; } = [];
    public Func<DocumentPickerMode, Document, bool> IsSupported { get; set; } =
        (mode, document) => mode == DocumentPickerMode.All
            || Path.GetExtension(document.StoragePath ?? string.Empty).Equals(".png", StringComparison.OrdinalIgnoreCase)
            || Path.GetExtension(document.StoragePath ?? string.Empty).Equals(".jpg", StringComparison.OrdinalIgnoreCase);

    public Task<string?> PickAsync(DocumentPickerMode mode) => Task.FromResult(NextPick);
    public Task<List<string>> PicksAsync(DocumentPickerMode mode) => Task.FromResult(NextPicks);
    public bool IsSupportedFile(DocumentPickerMode mode, Document document) => IsSupported(mode, document);
}

internal sealed class FakeWoolService : IWoolService
{
    public ResultT<IReadOnlyList<Wool>> GetAllResult { get; set; } = ResultT<IReadOnlyList<Wool>>.Ok([]);
    public Dictionary<int, ResultT<Wool>> ByIdResults { get; } = [];
    public ResultT<Wool> AddResult { get; set; } = ResultT<Wool>.Ok(TestData.Wool());
    public ResultT<Wool> UpdateResult { get; set; } = ResultT<Wool>.Ok(TestData.Wool());
    public Result DeleteResult { get; set; } = Result.Ok();
    public Result AddStockResult { get; set; } = Result.Ok();
    public int GetAllCalls { get; private set; }
    public List<CreateWoolRequest> AddRequests { get; } = [];
    public List<UpdateWoolRequest> UpdateRequests { get; } = [];
    public List<int> DeleteIds { get; } = [];
    public List<(int Id, double Quantity)> AddStockRequests { get; } = [];
    public List<(int Id, double Quantity, int? ProjectId)> AddStockWithProjectRequests { get; } = [];

    public Task<ResultT<IReadOnlyList<Wool>>> GetAllAsync()
    {
        GetAllCalls++;
        return Task.FromResult(GetAllResult);
    }

    public Task<ResultT<Wool>> GetByIdAsync(int id) =>
        Task.FromResult(ByIdResults.GetValueOrDefault(id, ResultT<Wool>.NotFound("not found")));

    public Task<ResultT<Wool>> AddAsync(CreateWoolRequest request)
    {
        AddRequests.Add(request);
        return Task.FromResult(AddResult);
    }

    public Task<ResultT<Wool>> UpdateAsync(UpdateWoolRequest request)
    {
        UpdateRequests.Add(request);
        return Task.FromResult(UpdateResult);
    }

    public Task<Result> DeleteAsync(int id)
    {
        DeleteIds.Add(id);
        return Task.FromResult(DeleteResult);
    }

    public Task<Result> AddStockAsync(int id, double quantity)
    {
        AddStockRequests.Add((id, quantity));
        return Task.FromResult(AddStockResult);
    }

    public Task<Result> AddStockAsync(int id, double quantity, int? projectId)
    {
        AddStockWithProjectRequests.Add((id, quantity, projectId));
        return AddStockAsync(id, quantity);
    }
}

internal sealed class FakePatternService : IPatternService
{
    public ResultT<IReadOnlyList<Pattern>> GetAllResult { get; set; } = ResultT<IReadOnlyList<Pattern>>.Ok([]);
    public Dictionary<int, ResultT<Pattern>> ByIdResults { get; } = [];
    public ResultT<Pattern> AddResult { get; set; } = ResultT<Pattern>.Ok(TestData.Pattern());
    public ResultT<Pattern> UpdateResult { get; set; } = ResultT<Pattern>.Ok(TestData.Pattern());
    public Result AddDocumentResult { get; set; } = Result.Ok();
    public Result RemoveDocumentResult { get; set; } = Result.Ok();
    public Result DeleteResult { get; set; } = Result.Ok();
    public int GetAllCalls { get; private set; }
    public List<CreatePatternRequest> AddRequests { get; } = [];
    public List<UpdatePatternRequest> UpdateRequests { get; } = [];
    public List<int> DeleteIds { get; } = [];

    public Task<ResultT<IReadOnlyList<Pattern>>> GetAllAsync()
    {
        GetAllCalls++;
        return Task.FromResult(GetAllResult);
    }

    public Task<ResultT<Pattern>> GetByIdAsync(int id) =>
        Task.FromResult(ByIdResults.GetValueOrDefault(id, ResultT<Pattern>.NotFound("not found")));

    public Task<ResultT<Pattern>> AddAsync(CreatePatternRequest request)
    {
        AddRequests.Add(request);
        return Task.FromResult(AddResult);
    }

    public Task<ResultT<Pattern>> UpdateAsync(UpdatePatternRequest request)
    {
        UpdateRequests.Add(request);
        return Task.FromResult(UpdateResult);
    }

    public Task<Result> AddDocumentAsync(int patternId, Guid documentId) => Task.FromResult(AddDocumentResult);
    public Task<Result> RemoveDocumentAsync(int patternId, Guid documentId) => Task.FromResult(RemoveDocumentResult);

    public Task<Result> DeleteAsync(int id)
    {
        DeleteIds.Add(id);
        return Task.FromResult(DeleteResult);
    }
}

internal sealed class FakeProjectService : IProjectService
{
    public ResultT<IReadOnlyList<Project>> GetAllResult { get; set; } = ResultT<IReadOnlyList<Project>>.Ok([]);
    public Dictionary<int, ResultT<Project>> ByIdResults { get; } = [];
    public ResultT<Project> AddResult { get; set; } = ResultT<Project>.Ok(TestData.Project());
    public ResultT<Project> UpdateResult { get; set; } = ResultT<Project>.Ok(TestData.Project());
    public Result DeleteResult { get; set; } = Result.Ok();
    public int GetAllCalls { get; private set; }
    public List<CreateProjectRequest> AddRequests { get; } = [];
    public List<UpdateProjectRequest> UpdateRequests { get; } = [];
    public List<int> DeleteIds { get; } = [];

    public Task<ResultT<IReadOnlyList<Project>>> GetAllAsync()
    {
        GetAllCalls++;
        return Task.FromResult(GetAllResult);
    }

    public Task<ResultT<Project>> GetByIdAsync(int id) =>
        Task.FromResult(ByIdResults.GetValueOrDefault(id, ResultT<Project>.NotFound("not found")));

    public Task<ResultT<Project>> AddAsync(CreateProjectRequest request)
    {
        AddRequests.Add(request);
        return Task.FromResult(AddResult);
    }

    public Task<ResultT<Project>> UpdateAsync(UpdateProjectRequest request)
    {
        UpdateRequests.Add(request);
        return Task.FromResult(UpdateResult);
    }

    public Task<Result> DeleteAsync(int id)
    {
        DeleteIds.Add(id);
        return Task.FromResult(DeleteResult);
    }
}

internal sealed class FakeDocumentService : IDocumentService
{
    public ResultT<IReadOnlyList<Document>> GetAllResult { get; set; } = ResultT<IReadOnlyList<Document>>.Ok([]);
    public Dictionary<Guid, ResultT<Document>> ByIdResults { get; } = [];
    public ResultT<Document> AddResult { get; set; } = ResultT<Document>.Ok(TestData.Document());
    public ResultT<IReadOnlyList<Document>> AddAllResult { get; set; } = ResultT<IReadOnlyList<Document>>.Ok([]);
    public ResultT<Document> UpdateResult { get; set; } = ResultT<Document>.Ok(TestData.Document());
    public Result DeleteResult { get; set; } = Result.Ok();
    public Result OpenResult { get; set; } = Result.Ok();
    public int GetAllCalls { get; private set; }
    public List<CreateDocumentRequest> AddRequests { get; } = [];
    public List<IReadOnlyList<CreateDocumentRequest>> AddAllRequests { get; } = [];
    public List<UpdateDocumentRequest> UpdateRequests { get; } = [];
    public List<Guid> DeleteIds { get; } = [];
    public List<Guid> OpenIds { get; } = [];

    public Task<ResultT<IReadOnlyList<Document>>> GetAllAsync()
    {
        GetAllCalls++;
        return Task.FromResult(GetAllResult);
    }

    public Task<ResultT<Document>> GetByIdAsync(Guid id) =>
        Task.FromResult(ByIdResults.GetValueOrDefault(id, ResultT<Document>.NotFound("not found")));

    public Task<ResultT<Document>> AddAsync(CreateDocumentRequest request)
    {
        AddRequests.Add(request);
        return Task.FromResult(AddResult);
    }

    public Task<ResultT<IReadOnlyList<Document>>> AddAllAsync(IReadOnlyList<CreateDocumentRequest> requests)
    {
        AddAllRequests.Add(requests);
        return Task.FromResult(AddAllResult);
    }

    public Task<ResultT<Document>> UpdateAsync(UpdateDocumentRequest request)
    {
        UpdateRequests.Add(request);
        return Task.FromResult(UpdateResult);
    }

    public Task<Result> DeleteAsync(Guid id)
    {
        DeleteIds.Add(id);
        return Task.FromResult(DeleteResult);
    }

    public Task<Result> OpenAsync(Guid id)
    {
        OpenIds.Add(id);
        return Task.FromResult(OpenResult);
    }
}

internal sealed class FakeWoolStockService : IWoolStockService
{
    public Result AdjustResult { get; set; } = Result.Ok();
    public List<AdjustProjectWoolUsageRequest> AdjustRequests { get; } = [];

    public Task<Result> AdjustWoolUsageAsync(AdjustProjectWoolUsageRequest request)
    {
        AdjustRequests.Add(request);
        return Task.FromResult(AdjustResult);
    }
}
