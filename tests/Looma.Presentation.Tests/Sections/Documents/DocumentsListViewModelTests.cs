using System.Collections.ObjectModel;
using FluentAssertions;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Base;
using Looma.Presentation.ViewModels.Sections.Documents;
using CommunityToolkit.Mvvm.Input;

namespace Looma.Presentation.Tests.Sections.Documents;

public class DocumentsListViewModelTests
{
    [Fact]
    public async Task ShouldPaginateTheDocumentsLikeTheWoolList()
    {
        var repo = new FakeDocumentRepository(CreateDocuments(13));
        var nav = new FakeNavigationService();
        var notifications = new FakeNotificationService();
        var vm = new DocumentsListViewModel(nav, repo, notifications);

        await vm.RefreshAsync();

        vm.CurrentPage.Should().Be(1);
        vm.TotalPages.Should().Be(2);
        vm.HasPreviousPage.Should().BeFalse();
        vm.HasNextPage.Should().BeTrue();
        vm.PageInfo.Should().Be("1 / 2");
        vm.CurrentPageDocuments.Should().HaveCount(12);

        vm.NextPageCommand.Execute(null);

        vm.CurrentPage.Should().Be(2);
        vm.HasPreviousPage.Should().BeTrue();
        vm.HasNextPage.Should().BeFalse();
        vm.PageInfo.Should().Be("2 / 2");
        vm.CurrentPageDocuments.Should().HaveCount(1);
        vm.CurrentPageDocuments.Single().Document.Nickname.Should().Be("Document 9");
    }

    [Fact]
    public async Task ShouldResetToFirstPageWhenSearching()
    {
        var repo = new FakeDocumentRepository(CreateDocuments(13));
        var nav = new FakeNavigationService();
        var notifications = new FakeNotificationService();
        var vm = new DocumentsListViewModel(nav, repo, notifications);

        await vm.RefreshAsync();
        vm.NextPageCommand.Execute(null);
        vm.CurrentPage.Should().Be(2);

        vm.SearchQuery = "Document 1";

        vm.CurrentPage.Should().Be(1);
        vm.TotalPages.Should().Be(1);
        vm.PageInfo.Should().Be("1 / 1");
        vm.CurrentPageDocuments.Should().HaveCount(5);
        vm.CurrentPageDocuments.Select(d => d.Document.Nickname)
            .Should().Equal("Document 1", "Document 10", "Document 11", "Document 12", "Document 13");
    }

    [Fact]
    public async Task ShouldNavigateToTheFormWhenAdding()
    {
        var repo = new FakeDocumentRepository(CreateDocuments(1));
        var nav = new FakeNavigationService();
        var notifications = new FakeNotificationService();
        var vm = new DocumentsListViewModel(nav, repo, notifications);

        await vm.RefreshAsync();

        vm.OpenAddFormCommand.Execute(null);

        nav.LastNavigatedType.Should().Be(typeof(DocumentsFormViewModel));
        nav.NavigateCount.Should().Be(1);
    }

    [Fact]
    public async Task ShouldDeleteTheDocumentAndReloadTheList()
    {
        var repo = new FakeDocumentRepository(CreateDocuments(2));
        var nav = new FakeNavigationService();
        var notifications = new FakeNotificationService();
        var vm = new DocumentsListViewModel(nav, repo, notifications);

        await vm.RefreshAsync();

        var first = vm.CurrentPageDocuments.First();
        await ((IAsyncRelayCommand)first.DeleteCommand).ExecuteAsync(null);

        repo.Documents.Should().HaveCount(1);
        vm.CurrentPageDocuments.Should().HaveCount(1);
        notifications.SuccessMessages.Should().ContainSingle();
    }

    private static IReadOnlyList<Document> CreateDocuments(int count) =>
        Enumerable.Range(1, count)
            .Select(index => new Document
            {
                Id = Guid.Parse($"00000000-0000-0000-0000-{index:D12}"),
                Nickname = $"Document {index}",
                Type = "PDF",
                SizeBytes = 1024L * index
            })
            .ToList();

    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        public List<Document> Documents { get; }

        public FakeDocumentRepository(IEnumerable<Document> documents)
        {
            Documents = documents.ToList();
        }

        public Task<ResultT<IReadOnlyList<Document>>> GetAllAsync() =>
            Task.FromResult(ResultT<IReadOnlyList<Document>>.Ok(
                Documents
                    .OrderBy(document => document.Nickname)
                    .ThenBy(document => document.Id)
                    .ToList()));

        public Task<ResultT<Document>> GetByIdAsync(Guid id)
        {
            var document = Documents.FirstOrDefault(d => d.Id == id);
            return Task.FromResult(document is null
                ? ResultT<Document>.NotFound($"Le document {id} est introuvable.")
                : ResultT<Document>.Ok(document));
        }

        public Task<ResultT<Document>> AddAsync(CreateDocumentRequest request)
        {
            var document = new Document
            {
                Id = Guid.NewGuid(),
                Nickname = string.IsNullOrWhiteSpace(request.Nickname)
                    ? Path.GetFileNameWithoutExtension(request.SourcePath)
                    : request.Nickname.Trim(),
                Type = Path.GetExtension(request.SourcePath).TrimStart('.').ToUpperInvariant(),
                SizeBytes = 1
            };

            Documents.Add(document);
            return Task.FromResult(ResultT<Document>.Ok(document));
        }

        public Task<ResultT<Document>> UpdateAsync(UpdateDocumentRequest request)
        {
            var document = Documents.FirstOrDefault(d => d.Id == request.Id);
            if (document is null)
                return Task.FromResult(ResultT<Document>.NotFound($"Le document {request.Id} est introuvable."));

            var updated = new Document
            {
                Id = document.Id,
                Nickname = request.Nickname.Trim(),
                Type = document.Type,
                SizeBytes = document.SizeBytes
            };

            Documents[Documents.IndexOf(document)] = updated;
            return Task.FromResult(ResultT<Document>.Ok(updated));
        }

        public Task<Result> DeleteAsync(Guid id)
        {
            var document = Documents.FirstOrDefault(d => d.Id == id);
            if (document is null)
                return Task.FromResult(Result.NotFound($"Le document {id} est introuvable."));

            Documents.Remove(document);
            return Task.FromResult(Result.Ok());
        }

        public Task<Result> OpenAsync(Guid id) => Task.FromResult(Result.Ok());
    }

    private sealed class FakeNavigationService : INavigationService
    {
        public PageViewModelBase? CurrentPage => null;
        public bool CanGoBack => false;
        public int NavigateCount { get; private set; }
        public Type? LastNavigatedType { get; private set; }

        public event EventHandler<PageViewModelBase>? Navigated
        {
            add { }
            remove { }
        }

        public void NavigateTo<TViewModel>(Action<TViewModel>? configure = null) where TViewModel : PageViewModelBase
        {
            NavigateCount++;
            LastNavigatedType = typeof(TViewModel);
        }

        public void PushPage(PageViewModelBase page)
        {
        }

        public void GoBack()
        {
        }

        public void ClearHistory()
        {
        }
    }

    private sealed class FakeNotificationService : INotificationService
    {
        public IReadOnlyList<NotificationItemViewModel> Notifications => [];
        public List<string> SuccessMessages { get; } = [];
        public List<string> ErrorMessages { get; } = [];

        public NotificationItemViewModel Info(string message, string? title = null, TimeSpan? duration = null) =>
            Create(NotificationSeverity.Info, message, title);

        public NotificationItemViewModel Success(string message, string? title = null, TimeSpan? duration = null)
        {
            SuccessMessages.Add(message);
            return Create(NotificationSeverity.Success, message, title);
        }

        public NotificationItemViewModel Warning(string message, string? title = null, TimeSpan? duration = null) =>
            Create(NotificationSeverity.Warning, message, title);

        public NotificationItemViewModel Error(string message, string? title = null, TimeSpan? duration = null)
        {
            ErrorMessages.Add(message);
            return Create(NotificationSeverity.Error, message, title);
        }

        public void Dismiss(Guid id)
        {
        }

        public void Clear()
        {
        }

        private static NotificationItemViewModel Create(NotificationSeverity severity, string message, string? title) =>
            new(Guid.NewGuid(), severity, title ?? string.Empty, message, _ => { });
    }
}
