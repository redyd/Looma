using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Looma.Presentation.Notifications;

public partial class NotificationItemViewModel : ObservableObject
{
    private readonly Action<NotificationItemViewModel> _dismiss;

    public Guid Id { get; }
    public NotificationSeverity Severity { get; }
    public string Title { get; }
    public string Message { get; }
    public string IconGlyph { get; }
    public IBrush AccentBrush { get; }
    public IBrush BackgroundBrush { get; }
    public IBrush BorderBrush { get; }

    public NotificationItemViewModel(
        Guid id,
        NotificationSeverity severity,
        string title,
        string message,
        Action<NotificationItemViewModel> dismiss)
    {
        Id = id;
        Severity = severity;
        Title = title;
        Message = message;
        _dismiss = dismiss;

        (IconGlyph, AccentBrush, BackgroundBrush, BorderBrush) = severity switch
        {
            NotificationSeverity.Success => ("✓", Brush("#16A34A"), Brush("#ECFDF5"), Brush("#BBF7D0")),
            NotificationSeverity.Warning => ("!", Brush("#D97706"), Brush("#FFFBEB"), Brush("#FDE68A")),
            NotificationSeverity.Error => ("×", Brush("#DC2626"), Brush("#FEF2F2"), Brush("#FECACA")),
            _ => ("i", Brush("#2563EB"), Brush("#EFF6FF"), Brush("#BFDBFE"))
        };
    }

    [RelayCommand]
    private void Dismiss() => _dismiss(this);

    private static SolidColorBrush Brush(string hex) => new(Color.Parse(hex));
}
