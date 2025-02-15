using APIMonitor.server.Data;
using APIMonitor.server.Hubs;
using APIMonitor.server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace APIMonitor.server.Services.NotificationsService;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IHubContext<NotificationHub> hubContext;

    public NotificationService(ApplicationDbContext dbContext, IHubContext<NotificationHub> hubContext)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }
    
    public async Task<List<Notification>> GetUserNotifications(string userId, bool onlyUnread)
    {
        return await dbContext.Notifications
            .Where(n => n.UserId == Convert.ToInt32(userId) && (!onlyUnread || !n.IsRead))
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task MarkAsRead(Guid notificationId)
    {
        Notification? notification = await dbContext.Notifications.FindAsync(notificationId);

        if (notification is not null)
        {
            notification.IsRead = true;
            
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task SendNotificationAsync(string userId, string title, string message)
    {
       ArgumentNullException.ThrowIfNull(userId);
       ArgumentNullException.ThrowIfNull(message);

       Notification notification = new()
       {
           UserId = Convert.ToInt32(userId),
           Title = title,
           Message = message,
           IsRead = false,
           CreatedAt = DateTime.UtcNow
       };
       
       await dbContext.Notifications.AddAsync(notification);
       await dbContext.SaveChangesAsync();
       
       await hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", title, message);
    }
}