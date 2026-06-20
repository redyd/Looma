// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Looma.Presentation.Notifications;

public class NotificationService : ObservableObject, INotificationService
{
    private static readonly TimeSpan DismissAnimationDuration = TimeSpan.FromMilliseconds(180);

    private readonly ObservableCollection<NotificationItemViewModel> _notifications = [];
    private readonly Dictionary<Guid, CancellationTokenSource> _timers = new();
    private readonly object _gate = new();

    public IReadOnlyList<NotificationItemViewModel> Notifications => _notifications;

    public NotificationItemViewModel Info(string message, string? title = null, TimeSpan? duration = null) =>
        Add(NotificationSeverity.Info, message, title ?? "Information", duration);

    public NotificationItemViewModel Success(string message, string? title = null, TimeSpan? duration = null) =>
        Add(NotificationSeverity.Success, message, title ?? "Succès", duration);

    public NotificationItemViewModel Warning(string message, string? title = null, TimeSpan? duration = null) =>
        Add(NotificationSeverity.Warning, message, title ?? "Attention", duration);

    public NotificationItemViewModel Error(string message, string? title = null, TimeSpan? duration = null) =>
        Add(NotificationSeverity.Error, message, title ?? "Erreur", duration);

    public void Dismiss(Guid id)
    {
        CancellationTokenSource? cts;

        lock (_gate)
        {
            if (!_timers.TryGetValue(id, out cts))
                cts = null;

            _timers.Remove(id);
        }

        cts?.Cancel();
        cts?.Dispose();

        Dispatcher.UIThread.Post(() => StartDismissAnimation(id));
    }

    public void Clear()
    {
        lock (_gate)
        {
            foreach (var timer in _timers.Values)
                timer.Cancel();

            foreach (var timer in _timers.Values)
                timer.Dispose();

            _timers.Clear();
        }

        Dispatcher.UIThread.Post(() => _notifications.Clear());
    }

    private NotificationItemViewModel Add(
        NotificationSeverity severity,
        string message,
        string title,
        TimeSpan? duration)
    {
        var item = new NotificationItemViewModel(
            Guid.NewGuid(),
            severity,
            title,
            message,
            notification => Dismiss(notification.Id));
        var timeout = duration ?? TimeSpan.FromSeconds(4);

        Dispatcher.UIThread.Post(() => { _notifications.Insert(0, item); });

        if (timeout > TimeSpan.Zero)
        {
            var cts = new CancellationTokenSource();
            lock (_gate)
            {
                _timers[item.Id] = cts;
            }

            _ = AutoDismissAsync(item.Id, timeout, cts.Token);
        }

        return item;
    }

    private async Task AutoDismissAsync(Guid id, TimeSpan timeout, CancellationToken token)
    {
        try
        {
            await Task.Delay(timeout, token);
            if (!token.IsCancellationRequested)
                Dismiss(id);
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async void StartDismissAnimation(Guid id)
    {
        var item = _notifications.FirstOrDefault(n => n.Id == id);
        if (item is null || item.IsDismissing)
            return;

        item.IsDismissing = true;
        await Task.Delay(DismissAnimationDuration);

        _notifications.Remove(item);
    }
}
