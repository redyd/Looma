// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Avalonia.Media;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Looma.Presentation.Notifications;

public partial class NotificationItemViewModel : ObservableObject
{
    private readonly Action<NotificationItemViewModel> _dismiss;
    [ObservableProperty]
    private bool _isDismissing;

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
            NotificationSeverity.Success => (
                "CircleCheckBig",
                ResourceBrush("NotificationSuccessAccentBrush", "#16A34A"),
                ResourceBrush("NotificationSuccessBackgroundBrush", "#ECFDF5"),
                ResourceBrush("NotificationSuccessBorderBrush", "#BBF7D0")),
            NotificationSeverity.Warning => (
                "TriangleAlert",
                ResourceBrush("NotificationWarningAccentBrush", "#D97706"),
                ResourceBrush("NotificationWarningBackgroundBrush", "#FFFBEB"),
                ResourceBrush("NotificationWarningBorderBrush", "#FDE68A")),
            NotificationSeverity.Error => (
                "OctagonX",
                ResourceBrush("NotificationErrorAccentBrush", "#DC2626"),
                ResourceBrush("NotificationErrorBackgroundBrush", "#FEF2F2"),
                ResourceBrush("NotificationErrorBorderBrush", "#FECACA")),
            _ => (
                "BadgeInfo",
                ResourceBrush("NotificationInfoAccentBrush", "#2563EB"),
                ResourceBrush("NotificationInfoBackgroundBrush", "#EFF6FF"),
                ResourceBrush("NotificationInfoBorderBrush", "#BFDBFE"))
        };
    }

    [RelayCommand]
    private void Dismiss() => _dismiss(this);

    private static IBrush ResourceBrush(string key, string fallback)
    {
        if (Application.Current?.TryGetResource(key, null, out var value) == true)
        {
            return value switch
            {
                IBrush brush => brush,
                Color color => new SolidColorBrush(color),
                _ => Brush(fallback)
            };
        }

        return Brush(fallback);
    }

    private static SolidColorBrush Brush(string hex) => new(Color.Parse(hex));
}
