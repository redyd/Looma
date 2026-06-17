// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

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
    public string IconKind { get; }
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

        (IconKind, AccentBrush, BackgroundBrush, BorderBrush) = severity switch
        {
            NotificationSeverity.Success => ("CircleCheckBig", Brush("#16A34A"), Brush("#ECFDF5"), Brush("#BBF7D0")),
            NotificationSeverity.Warning => ("TriangleAlert", Brush("#D97706"), Brush("#FFFBEB"), Brush("#FDE68A")),
            NotificationSeverity.Error => ("OctagonX", Brush("#DC2626"), Brush("#FEF2F2"), Brush("#FECACA")),
            _ => ("BadgeInfo", Brush("#2563EB"), Brush("#EFF6FF"), Brush("#BFDBFE"))
        };
    }

    [RelayCommand]
    private void Dismiss() => _dismiss(this);

    private static SolidColorBrush Brush(string hex) => new(Color.Parse(hex));
}
