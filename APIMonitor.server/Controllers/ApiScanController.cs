using System.Diagnostics;
using System.Security.Claims;
using APIMonitor.server.Hubs;
using APIMonitor.server.Models;
using APIMonitor.server.Services.ApiScannerService;
using APIMonitor.server.Services.AuditLogService;
using APIMonitor.server.Services.NotificationsService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace APIMonitor.server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApiScanController : ControllerBase 
{
    private readonly IApiScannerService apiScannerService;
    private readonly IHubContext<NotificationHub> hubContext;
    private readonly IAuditLogService auditLogService;
    private readonly INotificationService notificationService;

    public ApiScanController(IApiScannerService apiScannerService, IHubContext<NotificationHub> hubContext, IAuditLogService auditLogService, INotificationService notificationService)
    {
        this.apiScannerService = apiScannerService ?? throw new ArgumentNullException(nameof(apiScannerService));
        this.hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        this.auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    [HttpPost("scan")]
    public async Task<IActionResult> ScanApis([FromHeader]string id)
    {
        await apiScannerService.ScanApisAsync();

        await hubContext.Clients.User(id).SendAsync("ReceiveNotification", "API scan triggered!");
        
        return Ok(new { message = "Manual API scan triggered successfully!" });
    }
    
    [Authorize] 
    [HttpPost("scan-single")]
    public async Task<IActionResult> ScanSingleApi([FromQuery] string apiUrl)
    {
        DateTime startTime = DateTime.UtcNow;
        
        if (string.IsNullOrEmpty(apiUrl))
        {
            return BadRequest(new { message = "API URL is required" });
        }
        
        string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid authentication." });
        }
        
        try
        {
            using HttpClient client = new();
            
            Stopwatch stopwatch = Stopwatch.StartNew();
            HttpResponseMessage response = await client.GetAsync(apiUrl);
            stopwatch.Stop();

            var result = new
            {
                totalRequests = 1,
                averageResponseTime = stopwatch.Elapsed.TotalMilliseconds,
                errorsCount = response.IsSuccessStatusCode ? 0 : 1
            };
            
            await auditLogService.LogActionAsync(int.Parse(userId), "ScanSingleApi", $"Scanned {apiUrl} - Success: {response.IsSuccessStatusCode}", startTime);
            await notificationService.SendNotificationAsync(userId, "API Scan", $"Scanned {apiUrl}: {result.errorsCount} errors.", HttpContext);

            return Ok(result);
        }
        catch (Exception ex)
        {
            await auditLogService.LogActionAsync(int.Parse(userId), "ScanSingleApiFailed", $"Failed to scan {apiUrl}: {ex.Message}", startTime);
            await notificationService.SendNotificationAsync(userId, "Scan Failed", $"Failed to scan {apiUrl}: {ex.Message}", HttpContext);
            
            return StatusCode(500, new { message = $"Scan failed: {ex.Message}" });
        }
    }
}