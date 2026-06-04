namespace Looma.Presentation.Services;

public interface IDataRefreshService
{
    event EventHandler? DocumentsRefreshRequested;
    event EventHandler? PatternsRefreshRequested;

    void RequestDocumentsRefresh();
    void RequestPatternsRefresh();
}
