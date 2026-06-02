using CommunityToolkit.Mvvm.ComponentModel;
using Looma.Domain.Entities;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public partial class PatternDocumentSelectionViewModel : ObservableObject
{
    public PatternDocumentSelectionViewModel(Document document, bool isSelected = false)
    {
        Document = document;
        _isSelected = isSelected;
    }

    public Document Document { get; }

    [ObservableProperty]
    private bool _isSelected;

    public string TypeDisplay => Document.Type;
    public string SizeDisplay => FormatSize(Document.SizeBytes);
    public string DetailText => $"{TypeDisplay} · {SizeDisplay}";

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
