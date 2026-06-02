using System.Windows.Input;
using Looma.Domain.Entities;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public record PatternSummaryViewModel(
    Pattern Pattern,
    int DocumentCount,
    int ProjectCount,
    bool HasUrl,
    ICommand OpenDetailCommand,
    ICommand EditCommand,
    ICommand DeleteCommand)
{
    public string UrlDisplay => HasUrl ? "Oui" : "Non";
    public bool HasNote => !string.IsNullOrWhiteSpace(Pattern.Note);
    public string NotePreview
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Pattern.Note))
                return "Aucune note.";

            const int maxLength = 140;
            return Pattern.Note.Length <= maxLength
                ? Pattern.Note
                : $"{Pattern.Note[..(maxLength - 3)]}...";
        }
    }
}
