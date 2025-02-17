using System.Net.Sockets;
using APIMonitor.server.Data;
using APIMonitor.server.Models;
using Microsoft.EntityFrameworkCore;

namespace APIMonitor.server.Services.ThreatDetectionService;

public class ThreatDetectionService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IHttpContextAccessor httpContextAccessor;

    public ThreatDetectionService(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task<bool> IsIpBlocked()
    {
        HttpContext? context = this.httpContextAccessor.HttpContext;

        if (context is null)
        {
            throw new InvalidOperationException("HttpContext is not available.");
        }
        
        string? ipAddress = GetIpAddress(context, AddressFamily.InterNetwork);

        if (string.IsNullOrEmpty(ipAddress))
        {
            return false;
        }

        bool isBlocked = await dbContext.IpBlocks.AnyAsync(ip => ip.Ip == ipAddress);
        
        return isBlocked;
    }

    public async Task LogFailedAttemptAsync(string reason)
    {
        HttpContext? context = this.httpContextAccessor.HttpContext;

        if (context is null)
        {
            throw new InvalidOperationException("HttpContext is not available.");
        }

        string? ipAddress = GetIpAddress(context, AddressFamily.InterNetwork);
        string userAgent = GetUserAgent(context);

        if (!string.IsNullOrEmpty(ipAddress))
        {
            await dbContext.BotDetectionLogs.AddAsync(new BotDetectionLog
            {
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Description = reason,
                DetectedAt = DateTime.UtcNow,
            });
            
            await dbContext.SaveChangesAsync();
        }
    }

    private static string? GetIpAddress(HttpContext context, AddressFamily addressFamily)
    {
        return context.Connection.RemoteIpAddress is {} ipAddress && ipAddress.AddressFamily == addressFamily
            ? ipAddress.ToString()
            : null;
    }

    private static string GetUserAgent(HttpContext context)
    {
        return context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
    }
}