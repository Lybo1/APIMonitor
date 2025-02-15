using System.Security.Claims;
using APIMonitor.server.Models;
using APIMonitor.server.Services.NotificationsService;
using APIMonitor.server.Services.SecurityService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIMonitor.server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly ISecurityEventService securityEventService;
    private readonly INotificationService notificationService;

    public NotificationController(ISecurityEventService securityEventService, INotificationService notificationService)
    {
        this.securityEventService = securityEventService ?? throw new ArgumentNullException(nameof(securityEventService));
        this.notificationService = notificationService;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] bool onlyUnread = false)
    {
        string userId = GetUserId();

        List<Notification> notifications = await notificationService.GetUserNotifications(userId, onlyUnread);
        
        return Ok(notifications);
    }

    [Authorize]
    [HttpPost("mark-as-read/{id-guid}")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        await notificationService.MarkAsRead(id);
        
        return NoContent();
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier) ?.Value ?? throw new UnauthorizedAccessException("User not authenticated.");
    }
}