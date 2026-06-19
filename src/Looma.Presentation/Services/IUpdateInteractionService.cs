namespace Looma.Presentation.Services;

public interface IUpdateInteractionService
{
    event EventHandler? UpdatePromptRequested;
    event EventHandler? CurrentReleaseNotesRequested;

    void RequestUpdatePrompt();
    void RequestCurrentReleaseNotes();
}
