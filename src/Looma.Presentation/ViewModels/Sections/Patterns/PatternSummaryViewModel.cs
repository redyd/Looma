using System.Windows.Input;
using Looma.Domain.Entities;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public record PatternSummaryViewModel(
    Pattern Pattern,
    int DocumentCount,
    int ProjectCount,
    bool HasUrl,
    ICommand OpenDetailCommand)
{
    public string BeginDateDisplay => FormatDate(Pattern.BeginDate);
    public string EndDateDisplay => FormatDate(Pattern.EndDate);

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

    private static string FormatDate(DateOnly? value) =>
        value is null ? "Aucune" : value.Value.ToString("dd/MM/yyyy");
}
