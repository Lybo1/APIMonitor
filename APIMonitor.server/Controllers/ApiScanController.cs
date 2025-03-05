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

    [Authorize]
    [HttpPost("scan-single")]
    public async Task<IActionResult> ScanSingleApi(
        [FromQuery] string apiUrl, 
        [FromQuery] string method, 
        [FromQuery] string? apiKey = null, 
        [FromQuery] bool forceRefresh = false) // New param to bypass cache
    {
        if (string.IsNullOrEmpty(apiUrl))
        {
            _logger.LogWarning("ScanSingleApi attempted without API URL.");
            return BadRequest(new { message = "API URL is required" });
        }

        if (string.IsNullOrEmpty(method))
        {
            _logger.LogWarning("ScanSingleApi attempted without HTTP method.");
            return BadRequest(new { message = "HTTP method is required" });
        }

        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("ScanSingleApi attempted without valid user authentication.");
            return Unauthorized(new { message = "Invalid authentication" });
        }

        method = method.ToUpper();
        try
        {
            _ = new HttpMethod(method);
        }
        catch (ArgumentException)
        {
            _logger.LogWarning($"Invalid HTTP method '{method}' provided by userId: {userId}");
            return BadRequest(new { message = $"Invalid HTTP method: {method}. Use a valid HTTP verb (e.g., GET, POST, PATCH)." });
        }

        DateTime startTime = DateTime.UtcNow;
        _logger.LogInformation($"Starting single scan for {apiUrl} by userId: {userId} with method: {method}");
        await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", 
            $"[{startTime:O}] Initiating scan of {apiUrl} ({method})...");

        try
        {
            var (metrics, statusCode, responseSnippet) = await _apiScannerService.ScanSingleApiAsync(apiUrl, method, apiKey, userId, forceRefresh);
            int parsedId = int.TryParse(userId, out int id) ? id : 0;
            await _auditLogService.LogActionAsync(parsedId, "ScanSingleApi", 
                $"Successfully scanned {apiUrl} ({method}) - Status: {statusCode}", startTime);
            await _notificationService.SendNotificationAsync(userId, "API Scan", 
                $"Successfully scanned {apiUrl} ({method}) - Status: {statusCode}", HttpContext);

            _logger.LogInformation($"Completed single scan for {apiUrl} by userId: {userId} with method: {method}");
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", 
                $"[{DateTime.UtcNow:O}] Scan completed successfully for {apiUrl} ({method})");
            return Ok(new
            {
                message = "Scan completed successfully",
                totalResponseTime = metrics.AverageResponseTime.TotalMilliseconds,
                errorsCount = metrics.ErrorsCount,
                statusCode = (int)statusCode, // e.g., 200
                responseSnippet = responseSnippet // First 100 chars of response body
            });
        }
        catch (Exception ex)
        {
            int parsedId = int.TryParse(userId, out int id) ? id : 0;
            _logger.LogError(ex, $"Single scan failed for {apiUrl} by userId: {userId} with method: {method}");
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", 
                $"[{DateTime.UtcNow:O}] Scan failed for {apiUrl} ({method}): {ex.Message}");
            await _auditLogService.LogActionAsync(parsedId, "ScanSingleApiFailed", 
                $"Failed to scan {apiUrl}: {ex.Message} ({method})", startTime);
            await _notificationService.SendNotificationAsync(userId, "Scan Failed", 
                $"Failed to scan {apiUrl}: {ex.Message} ({method})", HttpContext);

            return StatusCode(500, new { message = $"Scan failed: {ex.Message}" });
        }
    }
}