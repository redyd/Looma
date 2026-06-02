using Looma.Domain.Entities;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public record PatternDocumentOptionViewModel(Document Document)
{
    public string DisplayText => Document.Nickname;
    public string DetailText => $"{Document.Type} · {FormatSize(Document.SizeBytes)}";

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0)
            return "0 B";

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var size = (double)bytes;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{bytes} {units[unitIndex]}"
            : $"{size:0.##} {units[unitIndex]}";
    }
}
