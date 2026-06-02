namespace Looma.Presentation.Notifications;

public interface INotificationService
{
    IReadOnlyList<NotificationItemViewModel> Notifications { get; }

    NotificationItemViewModel Info(string message, string? title = null, TimeSpan? duration = null);
    NotificationItemViewModel Success(string message, string? title = null, TimeSpan? duration = null);
    NotificationItemViewModel Warning(string message, string? title = null, TimeSpan? duration = null);
    NotificationItemViewModel Error(string message, string? title = null, TimeSpan? duration = null);

    void Dismiss(Guid id);
    void Clear();
}