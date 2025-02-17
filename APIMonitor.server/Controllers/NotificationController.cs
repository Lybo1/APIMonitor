using System.Security.Claims;
using APIMonitor.server.Data;
using APIMonitor.server.Models;
using APIMonitor.server.Services.NotificationsService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIMonitor.server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly ApplicationDbContext dbContext;
    private readonly INotificationService notificationService;

    public NotificationController(INotificationService notificationService, ApplicationDbContext dbContext)
    {
        this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [Authorize]
    [HttpGet("user")]
    public async Task<IActionResult> GetUserNotifications([FromHeader(Name = "Authorization")] string token, HttpContext context)
    {
        string userId = GetUserId();

        if (!await ValidateUserRequest(context, userId))
        {
            return Unauthorized(new { message = "Untrusted device or suspicious activity detected." });
        }

        List<Notification> notifications = await dbContext.Notifications
                                                          .AsNoTracking()
                                                          .Where(n => n.UserId.ToString() == userId)
                                                          .OrderByDescending(n => n.CreatedAt)
                                                          .ToListAsync();
        
        return Ok(notifications);
    }

    [Authorize]
    [HttpPost("mark-as-read")]
    public async Task<IActionResult> MarkAsRead([FromBody] int notificationId, HttpContext context)
    {
        string? userId = GetUserId();

        if (!await ValidateUserRequest(context, userId))
        {
            return Unauthorized(new { message = "Untrusted device or suspicious activity detected." });
            
        }
        
        int userIntId = Convert.ToInt32(userId);
        
        Notification? notification = await dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && userIntId  == n.UserId);

        if (notification is null)
        {
            return NotFound(new { message = "Notification not found." });
        }
        
        notification.IsRead = true;
        
        await dbContext.SaveChangesAsync();
        
        return Ok(new { message = "Notification marked as read." });
    }

    [HttpPost("send-test")]
    public async Task<IActionResult> SendTestNotification([FromBody] string message, HttpContext context)
    {
        string userId = GetUserId();

        if (!await ValidateUserRequest(context, userId))
        {
            return Unauthorized(new { message = "Untrusted device or suspicious activity detected." });
        }

        bool sent = await notificationService.SendNotificationAsync(userId,  "Test notification", message, context, isCritical: false);

        return sent
            ? Ok(new { message = "Test notification sent successfully." })
            : StatusCode(429, new { message = "Rate limit exceeded. Please wait before sending another notification." });
    }
    
    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException("User not authenticated.");
    }
    
    private async Task<bool> ValidateUserRequest(HttpContext context, string userId)
    {
        string? userIp = context.Items["UserIP"] as string;
        string? userAgent = context.Items["UserAgent"] as string;

        if (string.IsNullOrWhiteSpace(userIp) || string.IsNullOrWhiteSpace(userAgent))
        {
            return false;
        }

        bool isTrusted = await dbContext.TrustedDevices
            .AnyAsync(d => d.UserId == Convert.ToInt32(userId) && d.IpAddress == userIp && d.UserAgent == userAgent);

        return isTrusted;
    }
}