using System;
using System.Security.Claims;
using System.Threading.Tasks;
using APIMonitor.server.Hubs;
using APIMonitor.server.Services.ApiScannerService;
using APIMonitor.server.Services.AuditLogService;
using APIMonitor.server.Services.NotificationsService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace APIMonitor.server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApiScanController : ControllerBase
{
    private readonly IApiScannerService _apiScannerService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IAuditLogService _auditLogService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ApiScanController> _logger;

    public ApiScanController(
        IApiScannerService apiScannerService,
        IHubContext<NotificationHub> hubContext,
        IAuditLogService auditLogService,
        INotificationService notificationService,
        ILogger<ApiScanController> logger)
    {
        _apiScannerService = apiScannerService ?? throw new ArgumentNullException(nameof(apiScannerService));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // [HttpPost("scan")]
    // public async Task<IActionResult> ScanApis([FromHeader] string id)
    // {
    //     if (string.IsNullOrEmpty(id))
    //     {
    //         _logger.LogWarning("ScanApis attempted without user ID.");
    //         return BadRequest(new { message = "User ID header is required" });
    //     }
    //
    //     try
    //     {
    //         _logger.LogInformation($"Starting batch scan triggered by userId: {id}");
    //         await _hubContext.Clients.User(id).SendAsync("ReceiveNotification", $"[{DateTime.UtcNow:O}] Batch scan initiated for user {id}...");
    //         await _apiScannerService.ScanApisAsync();
    //         await _hubContext.Clients.User(id).SendAsync("ReceiveNotification", $"[{DateTime.UtcNow:O}] Batch scan completed.");
    //
    //         return Ok(new { message = "Manual API scan triggered successfully!" });
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, $"Batch scan failed for userId: {id}");
    //         await _hubContext.Clients.User(id).SendAsync("ReceiveNotification", $"[{DateTime.UtcNow:O}] Batch scan failed: {ex.Message}");
    //         return StatusCode(500, new { message = $"Batch scan failed: {ex.Message}" });
    //     }
    // }

    [Authorize]
[HttpPost("scan-single")]
public async Task<IActionResult> ScanSingleApi([FromQuery] string apiUrl, [FromQuery] string? method = "GET", [FromQuery] string? apiKey = null)
{
    if (string.IsNullOrEmpty(apiUrl))
    {
        _logger.LogWarning("ScanSingleApi attempted without API URL.");
        return BadRequest(new { message = "API URL is required" });
    }

    string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        _logger.LogWarning("ScanSingleApi attempted without valid user authentication.");
        return Unauthorized(new { message = "Invalid authentication" });
    }

    method = method?.ToUpper();
    if (!string.IsNullOrEmpty(method) && !new[] { "GET", "POST", "PUT", "DELETE" }.Contains(method))
    {
        _logger.LogWarning($"Invalid HTTP method '{method}' provided by userId: {userId}");
        return BadRequest(new { message = $"Invalid HTTP method: {method}. Use GET, POST, PUT, or DELETE." });
    }

    DateTime startTime = DateTime.UtcNow;
    _logger.LogInformation($"Starting single scan for {apiUrl} by userId: {userId}" + (method != null ? $" with method: {method}" : ""));
    await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", $"[{startTime:O}] Initiating scan of {apiUrl}" + (method != null ? $" ({method})..." : "..."));

    try
    {
        var metrics = await _apiScannerService.ScanSingleApiAsync(apiUrl, method, apiKey);
        int parsedId = int.TryParse(userId, out int id) ? id : 0;
        await _auditLogService.LogActionAsync(parsedId, "ScanSingleApi", $"Successfully scanned {apiUrl}" + (method != null ? $" ({method})" : ""), startTime);
        await _notificationService.SendNotificationAsync(userId, "API Scan", $"Successfully scanned {apiUrl}" + (method != null ? $" ({method})" : ""), HttpContext);

        _logger.LogInformation($"Completed single scan for {apiUrl} by userId: {userId}" + (method != null ? $" with method: {method}" : ""));
        return Ok(new
        {
            message = "Scan completed successfully",
            totalResponseTime = metrics.AverageResponseTime.TotalMilliseconds,
            errorsCount = metrics.ErrorsCount,
        });
    }
    catch (Exception ex)
    {
        int parsedId = int.TryParse(userId, out int id) ? id : 0;
        _logger.LogError(ex, $"Single scan failed for {apiUrl} by userId: {userId}" + (method != null ? $" with method: {method}" : ""));
        await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", $"[{DateTime.UtcNow:O}] Scan failed: {ex.Message}");
        await _auditLogService.LogActionAsync(parsedId, "ScanSingleApiFailed", $"Failed to scan {apiUrl}: {ex.Message}" + (method != null ? $" ({method})" : ""), startTime);
        await _notificationService.SendNotificationAsync(userId, "Scan Failed", $"Failed to scan {apiUrl}: {ex.Message}" + (method != null ? $" ({method})" : ""), HttpContext);

        return StatusCode(500, new { message = $"Scan failed: {ex.Message}" });

        }    
    }
}