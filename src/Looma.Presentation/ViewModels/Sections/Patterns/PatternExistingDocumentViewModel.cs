using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public partial class PatternExistingDocumentViewModel(
    Guid documentId,
    string nickname,
    string typeDisplay,
    string sizeDisplay,
    Action<PatternExistingDocumentViewModel>? removeRequested = null)
    : ObservableObject
{
    public Guid DocumentId { get; } = documentId;

    [ObservableProperty]
    private string _nickname = nickname;

    public string TypeDisplay { get; } = typeDisplay;
    public string SizeDisplay { get; } = sizeDisplay;
    public string DetailText => $"{TypeDisplay} · {SizeDisplay}";

    public string OriginalNickname { get; } = nickname;

    [RelayCommand]
    private void Remove() => removeRequested?.Invoke(this);
}
