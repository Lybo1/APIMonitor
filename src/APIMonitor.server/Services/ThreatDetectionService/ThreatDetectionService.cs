using System.Collections.Concurrent;
using System.Net.Sockets;
using APIMonitor.server.Data;
using APIMonitor.server.Data.Enumerations;
using APIMonitor.server.Hubs;
using APIMonitor.server.Models;
using APIMonitor.server.Services.ApiScannerService;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace APIMonitor.server.Services.ThreatDetectionService;

public class ThreatDetectionService : IThreatDetectionService
{
    private static readonly TimeSpan BanDuration = TimeSpan.FromHours(1);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);
    private static readonly ConcurrentDictionary<string, int> threatTracker = new();
    
    private readonly ApplicationDbContext dbContext;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IMemoryCache memoryCache;
    private readonly IHubContext<NotificationHub> hubContext;


    public ThreatDetectionService(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache, IHubContext<NotificationHub> hub)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        this.hubContext = hub ?? throw new ArgumentNullException(nameof(hub));
        
        Task.Run(UnbanExpiredIpsAsync);
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
        
        if (memoryCache.TryGetValue($"Blocked:{ipAddress}", out _))
        {
            return true;
        }
        
        IpBlock? ipBlock = await dbContext.IpBlocks.FirstOrDefaultAsync(ip => ip.Ip == ipAddress);
        
        return ipBlock is not null && ipBlock.BlockedUntil > DateTime.UtcNow;
    }

    public async Task LogFailedAttemptAsync(string action, string reason, AlertType alertType)
    {
        HttpContext? context = this.httpContextAccessor.HttpContext;
        
        if (context is null)
        {
            throw new InvalidOperationException("HttpContext is not available.");
        }
        
        string? ipAddress = GetIpAddress(context, AddressFamily.InterNetwork);
        
        if (string.IsNullOrEmpty(ipAddress))
        {
            return;
        }
        
        int attempts = threatTracker.AddOrUpdate(ipAddress, 1, (_, count) => count + 1);

        AlertSeverity severity = attempts switch
        {
            < 3 => AlertSeverity.Low,
            < 6 => AlertSeverity.Medium,
            _ => AlertSeverity.High
        };
        
        await LogThreatAsync(ipAddress, alertType, reason, severity);
        
        if (attempts >= Constants.MaxLoginAttempts)
        {
            await BanIpAsync(ipAddress, $"Exceeded {Constants.MaxLoginAttempts} failed attempts.");
        }
    }

    public async Task BanIpAsync(string ipAddress, string reason)
    {
        memoryCache.Set($"Blocked:{ipAddress}", true, BanDuration);
        
        IpBlock ipBlock = new()
        {
            Ip = ipAddress,
            BlockedUntil = DateTime.UtcNow.Add(BanDuration),
            FailedAttempts = Constants.MaxLoginAttempts,
            Reason = reason,
        };
        
        await dbContext.IpBlocks.AddAsync(ipBlock);
        await dbContext.SaveChangesAsync();
        
        await hubContext.Clients.All.SendAsync("ReceiveNotification", $"🚨 IP {ipAddress} has been banned!");
        
        await LogThreatAsync(ipAddress, AlertType.IpBanned, reason, AlertSeverity.High);
    }

    public async Task LogThreatAsync(string ip, AlertType alertType, string description, AlertSeverity severity)
    {
        HttpContext? context = this.httpContextAccessor.HttpContext;
        string? adminName = context?.User.Identity?.Name ?? "Unknown";
        
        ThreatAlert alert = new()
        {
            IpAddress = ip,
            AlertType = alertType,
            Description = $"{description} (by {adminName})",
            Severity = severity,
            IsResolved = false,
            TimeStamp = DateTime.UtcNow
        };

        await dbContext.ThreatAlerts.AddAsync(alert);
        await dbContext.SaveChangesAsync();
    }

    private async Task UnbanExpiredIpsAsync()
    {
        while (true)
        {
            await Task.Delay(CleanupInterval);

            List<IpBlock> expiredBans = await dbContext.IpBlocks
                .Where(ip => ip.BlockedUntil <= DateTime.UtcNow)
                .ToListAsync();

            if (expiredBans.Count == 0)
            {
                continue;
            }

            dbContext.IpBlocks.RemoveRange(expiredBans);
            await dbContext.SaveChangesAsync();

            foreach (IpBlock ipBlock in expiredBans)
            {
                memoryCache.Remove($"Blocked:{ipBlock.Ip}");
            }
        }
    }

    private static string? GetIpAddress(HttpContext context, AddressFamily addressFamily)
    {
        return context.Connection.RemoteIpAddress is { } ipAddress && ipAddress.AddressFamily == addressFamily
            ? ipAddress.ToString()
            : null;
    }
}
