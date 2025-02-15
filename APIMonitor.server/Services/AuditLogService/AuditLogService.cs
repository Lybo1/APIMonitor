using APIMonitor.server.Data;
using APIMonitor.server.Models;
using APIMonitor.server.Services.MacAddressService;

namespace APIMonitor.server.Services.AuditLogService;

public class AuditLogService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IMacAddressService macAddressService;
    private readonly IHttpContextAccessor httpContextAccessor;

    public AuditLogService(ApplicationDbContext dbContext, IMacAddressService macAddressService, IHttpContextAccessor httpContextAccessor)
    {
        this.dbContext = dbContext;
        this.macAddressService = macAddressService;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task LogActionAsync(int userId, string action, string details)
    {
        ArgumentNullException.ThrowIfNull(action, nameof(action));
        ArgumentNullException.ThrowIfNull(details, nameof(details));
        
        HttpContext? context = httpContextAccessor.HttpContext;

        if (context is null)
        {
            throw new InvalidOperationException("HttpContext is not available.");
        }
        
        string? macAddress = await macAddressService.GetMacAddressAsync(context);
        string ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        AuditLog log = new()
        {
            UserId = userId,
            Ipv4Address = ipAddress,
            MacAddress = macAddress ?? "unknown",
            Action = action,
            Details = details,
            Date = DateTime.UtcNow
        };
        
        await dbContext.AuditLogs.AddAsync(log);
        await dbContext.SaveChangesAsync();
    }
}