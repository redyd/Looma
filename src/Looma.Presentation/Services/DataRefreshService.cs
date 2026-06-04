namespace Looma.Presentation.Services;

public sealed class DataRefreshService : IDataRefreshService
{
    public event EventHandler? DocumentsRefreshRequested;
    public event EventHandler? PatternsRefreshRequested;

    public void RequestDocumentsRefresh() => DocumentsRefreshRequested?.Invoke(this, EventArgs.Empty);

    public void RequestPatternsRefresh() => PatternsRefreshRequested?.Invoke(this, EventArgs.Empty);
}
