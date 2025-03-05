using System.Net.Sockets;
using APIMonitor.server.Data;
using APIMonitor.server.Models;
using Microsoft.EntityFrameworkCore;

namespace APIMonitor.server.Services.AuditLogService;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IHttpContextAccessor httpContextAccessor;

    public AuditLogService(ApplicationDbContext dbContext, 
                           IHttpContextAccessor httpContextAccessor)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task LogActionAsync(int userId, string action, string details, DateTime requestStartTime)
    {
        ArgumentNullException.ThrowIfNull(action, nameof(action));
        ArgumentNullException.ThrowIfNull(details, nameof(details));
        
        HttpContext? context = httpContextAccessor.HttpContext;

        if (context is null)
        {
            throw new InvalidOperationException("HttpContext is not available.");
        }
        
        string ipv4Address = GetIpAddress(context, AddressFamily.InterNetwork) ?? "unknown";
        string ipv6Address = GetIpAddress(context, AddressFamily.InterNetworkV6) ?? "unknown";
        string userAgent = GetUserAgent(context);
        
        long responseTimeMs = (long)(DateTime.UtcNow - requestStartTime).TotalMilliseconds;

        AuditLog log = new()
        {
            UserId = userId,
            Ipv4Address = ipv4Address,
            Ipv6Address = ipv6Address,
            UserAgent = userAgent,
            Action = action,
            Details = details,
            RequestTimestamp = requestStartTime,
            ResponseTimeMs = responseTimeMs,
            Date = DateTime.UtcNow
        };
        
        await dbContext.AuditLogs.AddAsync(log);
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetUserAuditLogsAsync(int userId)
    {
        return await dbContext.AuditLogs
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.Date)
            .ToListAsync();
    }

    public async Task<AuditLog?> GetAuditLogByIdAsync(int id, int? userId = null)
    {
        return userId is null
            ? await dbContext.AuditLogs.FindAsync(id)
            : await dbContext.AuditLogs.FirstOrDefaultAsync(log => log.Id == id && log.UserId == userId);
    }

    public async Task<List<AuditLog>> GetAllAuditLogsAsync()
    {
        return await dbContext.AuditLogs.OrderByDescending(log => log.Date).ToListAsync();
    }

    public async Task<bool> DeleteAuditLogAsync(int id)
    {
        AuditLog? log = await dbContext.AuditLogs.FindAsync(id);
        if (log is null) return false;
        dbContext.AuditLogs.Remove(log);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PurgeAuditLogsAsync()
    {
        await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM AuditLogs");
        return true;
    }

    private static string? GetIpAddress(HttpContext context, AddressFamily family)
    {
        return context.Connection.RemoteIpAddress is { } ipAddress && ipAddress.AddressFamily == family ? ipAddress.ToString() : null;
    }

    private static string GetUserAgent(HttpContext context)
    {
        return context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
    }
}