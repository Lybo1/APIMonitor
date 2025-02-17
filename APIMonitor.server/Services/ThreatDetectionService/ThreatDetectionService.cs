using System.Net.Sockets;
using APIMonitor.server.Data;
using APIMonitor.server.Models;
using Microsoft.EntityFrameworkCore;

namespace APIMonitor.server.Services.ThreatDetectionService;

public class ThreatDetectionService : IThreatDetectionService
{
    private static readonly TimeSpan BanDuration = TimeSpan.FromHours(1);
    
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

        IpBlock? ipBlock = await dbContext.IpBlocks.FirstOrDefaultAsync(ip => ip.Ip == ipAddress);

        return ipBlock is not null && ipBlock.BlockedUntil > DateTime.UtcNow;
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

        if (string.IsNullOrEmpty(ipAddress))
        {
            return;
        }
        
        IpBlock? ipBlock = await dbContext.IpBlocks.FirstOrDefaultAsync(ip => ip.Ip == ipAddress);

        if (ipBlock is null)
        {
            ipBlock = new IpBlock
            {
                Ip = ipAddress,
                FailedAttempts = 1,
                BlockedUntil = DateTime.UtcNow,
                Reason = reason,
            };
            
            await dbContext.IpBlocks.AddAsync(ipBlock);
        }
        else
        {
            ipBlock.FailedAttempts++;

            if (ipBlock.FailedAttempts >= Constants.MaxLoginAttempts)
            {
                ipBlock.BlockedUntil = DateTime.UtcNow.Add(BanDuration);
                ipBlock.Reason = $"Exceeded {Constants.MaxLoginAttempts} failed attempts. Banned until {ipBlock.BlockedUntil}.";    
            }
            
            dbContext.IpBlocks.Update(ipBlock);
        }
        
        await dbContext.SaveChangesAsync();
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