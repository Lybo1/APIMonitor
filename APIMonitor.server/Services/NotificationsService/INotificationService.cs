using APIMonitor.server.Models;

namespace APIMonitor.server.Services.NotificationsService;

public interface INotificationService
{
    Task<List<Notification>> GetUserNotifications(string userId, bool onlyUnread);
    Task MarkAsRead(Guid notificationId);
    Task<bool> SendNotificationAsync(string userId, string title, string message, HttpContext context);
}