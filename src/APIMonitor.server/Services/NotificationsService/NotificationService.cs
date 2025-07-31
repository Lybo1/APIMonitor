using System.Collections.Concurrent;
using APIMonitor.server.Data;
using APIMonitor.server.Hubs;
using APIMonitor.server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace APIMonitor.server.Services.NotificationsService;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IHubContext<NotificationHub> hubContext;
    private readonly IMemoryCache cache;
    
    private static readonly ConcurrentDictionary<string, DateTime> LastNotificationTime = new();

    public NotificationService(ApplicationDbContext dbContext, IHubContext<NotificationHub> hubContext, IMemoryCache cache)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(string username)
    {
        if (string.IsNullOrEmpty(username))
            throw new ArgumentException("Username cannot be null or empty", nameof(username));

        if (!int.TryParse(username, out int userId))
            throw new ArgumentException("Invalid user ID format", nameof(username));

        return await dbContext.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
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

    public async Task<bool> SendNotificationAsync(string userId, string title, string message, HttpContext context, bool isCritical = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(context);
       
        string? userIp = context.Items["UserIP"] as string;
        string? userAgent = context.Items["UserAgent"] as string;

        if (string.IsNullOrWhiteSpace(userIp) || string.IsNullOrWhiteSpace(userAgent))
            return false;
       
        int userIdInt = Convert.ToInt32(userId);

        bool isTrusted = await dbContext.TrustedDevices
                                        .AnyAsync(d => d.UserId == userIdInt && d.IpAddress == userIp && d.UserAgent == userAgent);

        if (!isTrusted)
            return false;

        NotificationPreference? preferences = await dbContext.NotificationPreferences
                                                            .FirstOrDefaultAsync(p => p.UserId == userIdInt);

        if (preferences is not null)
        {
            if (preferences.EnableCriticalAlertsOnly && !isCritical)
                return false;
        }
        else
        {
            preferences = new NotificationPreference
            {
                UserId = userIdInt,
            };
           
            await dbContext.NotificationPreferences.AddAsync(preferences);
            await dbContext.SaveChangesAsync();
        }
       
        if (LastNotificationTime.TryGetValue(userId, out DateTime lastSentTime))
        {
            if ((DateTime.UtcNow - lastSentTime).TotalSeconds < 30)
                return false;
        }
       
        Notification notification = new()
        {
            UserId = userIdInt,
            Title = title,
            Message = message,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };
       
        await dbContext.Notifications.AddAsync(notification);
        await dbContext.SaveChangesAsync();
       
        LastNotificationTime[userId] = DateTime.UtcNow;
       
        await hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", title, message);
       
        return true;
    }
}