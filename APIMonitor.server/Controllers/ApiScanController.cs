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
            Stopwatch totalStopwatch = Stopwatch.StartNew();
            Stopwatch stepStopwatch = Stopwatch.StartNew();
            
            await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", $"Resolving and connecting to {apiUrl}");
            
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            stepStopwatch.Restart();
            
            await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", $"Sending GET request to {apiUrl}...");
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            
            stepStopwatch.Stop();
            
            double headersTime = stepStopwatch.Elapsed.TotalMilliseconds;
            await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", $"Headers received: {response.StatusCode} ({(int)response.StatusCode}) in {headersTime:F2}ms");

            stepStopwatch.Restart();
            
            await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", "Reading response body...");
            string responseBody = await response.Content.ReadAsStringAsync();
            
            stepStopwatch.Stop();

            double bodyTime = stepStopwatch.Elapsed.TotalMilliseconds;

            totalStopwatch.Stop();
            
            var result = new
            {
                totalRequests = 1,
                headersResponseTime = headersTime,
                totalResponseTime = headersTime + bodyTime, 
                errorsCount = response.IsSuccessStatusCode ? 0 : 1,
                statusCode = (int)response.StatusCode,
                responseBody = responseBody.Length > 500 ? responseBody.Substring(0, 500) + "..." : responseBody
            };
            
            await auditLogService.LogActionAsync(int.Parse(userId), "ScanSingleApi", $"Scanned {apiUrl} - Success: {response.IsSuccessStatusCode}", startTime);
            await notificationService.SendNotificationAsync(userId, "API Scan", $"Scanned {apiUrl}: {result.errorsCount} errors.", HttpContext);
            await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", $"Scan completed - Status: {result.statusCode}, Headers: {result.headersResponseTime:F2}ms, Total: {result.totalResponseTime:F2}ms, Errors: {result.errorsCount}");

            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", $"Scan failed: {ex.Message}");
            await auditLogService.LogActionAsync(int.Parse(userId), "ScanSingleApiFailed", $"Failed to scan {apiUrl}: {ex.Message}", startTime);
            await notificationService.SendNotificationAsync(userId, "Scan Failed", $"Failed to scan {apiUrl}: {ex.Message}", HttpContext);
            
            return StatusCode(500, new { message = $"Scan failed: {ex.Message}" });
        }
        catch (Exception ex)
        {
            await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", $"Unexpected error: {ex.Message}");
            await auditLogService.LogActionAsync(int.Parse(userId), "ScanSingleApiFailed", $"Failed to scan {apiUrl}: {ex.Message}", startTime);
            await notificationService.SendNotificationAsync(userId, "Scan Failed", $"Failed to scan {apiUrl}: {ex.Message}", HttpContext);
            
            return StatusCode(500, new { message = $"Scan failed: {ex.Message}" });
        }
    }
}