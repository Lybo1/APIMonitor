using System.Collections.Concurrent;
using APIMonitor.server.Data;
using APIMonitor.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace APIMonitor.server.Services.RateLimitService;

public class RateLimitService : IRateLimitService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IMemoryCache memoryCache;
    
    private static readonly ConcurrentDictionary<string, int> penaltyTracker = new();

    public RateLimitService(ApplicationDbContext dbContext, IMemoryCache memoryCache)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    public async Task<bool> IsRateLimitedAsync(HttpContext context, string action)
    {
        string userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";    
        string userIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        string cacheKey = $"{action}:{userId}:{userIp}";

        RateLimitRule? rule = await dbContext.RateLimitRules.FirstOrDefaultAsync(r => r.Action == action && r.IsActive);

        if (rule is null)
        {
            return false;
        }

        if (memoryCache.TryGetValue(cacheKey, out int requestCount))
        {
            if (requestCount >= rule.MaxRequests)
            {
                int penalty = penaltyTracker.GetOrAdd(cacheKey, 0);
                TimeSpan penaltyTime = rule.TimeWindow * (penalty + 1);
                
                penaltyTracker[cacheKey]++;
                
                memoryCache.Set(cacheKey, requestCount, penaltyTime);
                
                await LogViolationAsync(userId, userIp, action, penaltyTime);

                if (penalty >= Constants.MaxLoginAttempts)
                {
                    await AutoBanIpAsync(userIp);
                }
                
                return true;
            }
        }
        
        memoryCache.Set(cacheKey, requestCount + 1, rule.TimeWindow);
        
        return false;
    }

    private async Task LogViolationAsync(string userId, string userIp, string action, TimeSpan penaltyTime)
    {
        RateLimitViolation log = new()
        {
            UserId = userId,
            IpAddress = userIp,
            Action = action,
            Timestamp = DateTime.UtcNow,
            PenaltyDuration = penaltyTime
        };
        
        await dbContext.RateLimitViolations.AddAsync(log);
        await dbContext.SaveChangesAsync();
    }

    private async Task AutoBanIpAsync(string ipAddress)
    {
        bool isBanned = await dbContext.BannedIps.AnyAsync(ip => ip.IpAddress == ipAddress);
        
        if (isBanned)
        {
            return;
        }
        
        BannedIp ban = new()
        {
            IpAddress = ipAddress,
            BannedUntil = DateTime.UtcNow.AddHours(6),
            Reason = "Excessive rate-limit violations"
        };
        
        await dbContext.BannedIps.AddAsync(ban);
        await dbContext.SaveChangesAsync();
    }
}