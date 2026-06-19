namespace Looma.Presentation.Services;

public sealed class UpdateInteractionService : IUpdateInteractionService
{
    public event EventHandler? UpdatePromptRequested;
    public event EventHandler? CurrentReleaseNotesRequested;

    public void RequestUpdatePrompt() => UpdatePromptRequested?.Invoke(this, EventArgs.Empty);

    public void RequestCurrentReleaseNotes() => CurrentReleaseNotesRequested?.Invoke(this, EventArgs.Empty);
}
