using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using APIMonitor.server.Models;
using APIMonitor.server.Services.AuditLogService;
using APIMonitor.server.Services.GeoLocationService;

[Authorize(Roles = "User, Admin")]
[ApiController]
[Route("api/[controller]")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly IGeoLocationService _geoLocationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogController(
        IAuditLogService auditLogService,
        IGeoLocationService geoLocationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _geoLocationService = geoLocationService ?? throw new ArgumentNullException(nameof(geoLocationService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    [HttpGet("user-logs")]
    public async Task<IActionResult> GetUserAuditLogs()
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid authentication." });
        }

        int parsedUserId = int.Parse(userId);

        // Get client IP
        var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var forwardedFor = _httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            ipAddress = forwardedFor.Split(',')[0].Trim();
        }
        ipAddress ??= "Unknown";

        // Get geolocation data
        var (country, city, latitude, longitude, timeZone) = _geoLocationService.GetGeolocation(ipAddress);

        List<AuditLog> logs = await _auditLogService.GetUserAuditLogsAsync(parsedUserId);
        foreach (var log in logs)
        {
            if (log.Ipv4Address == ipAddress) // Only update current request's IP
            {
                log.Country = country;
                log.City = city;
                log.Latitude = latitude;
                log.Longitude = longitude;
                log.TimeZone = timeZone;
            }
        }

        return Ok(logs);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAuditLogById([FromRoute] int id)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid authentication." });
        }

        int parsedUserId = int.Parse(userId);
        AuditLog? log = await _auditLogService.GetAuditLogByIdAsync(id, parsedUserId);

        if (log is null)
        {
            return NotFound(new { message = "Log not found." });
        }

        if (string.IsNullOrEmpty(log.Country))
        {
            (string country, string city, double? latitude, double? longitude, string timeZone) = _geoLocationService.GetGeolocation(log.Ipv4Address);
            log.Country = country;
            log.City = city;
            log.Latitude = latitude;
            log.Longitude = longitude;
            log.TimeZone = timeZone;
        }

        return Ok(log);
    }
}