using System.Security.Claims;
using APIMonitor.server.Data;
using APIMonitor.server.Models;
using APIMonitor.server.Services.NotificationsService;
using APIMonitor.server.Services.SecurityService;
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
    public async Task<IActionResult> GetUserNotifications([FromHeader(Name = "Authorization")] string token)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid authentication." });
        }

        List<Notification> notifications = await dbContext.Notifications.Where(n => n.UserId.ToString() == userId)
                                                          .OrderByDescending(n => n.CreatedAt)
                                                          .ToListAsync();
        
        return Ok(notifications);
    }

    [Authorize]
    [HttpPost("mark-as-read")]
    public async Task<IActionResult> MarkAsRead([FromBody] int notificationId)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid authentication." });
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

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier) ?.Value ?? throw new UnauthorizedAccessException("User not authenticated.");
    }
}