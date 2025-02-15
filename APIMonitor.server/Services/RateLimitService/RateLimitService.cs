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
                
                return true;
            }
        }
        
        memoryCache.Set(cacheKey, requestCount + 1, rule.TimeWindow);
        
        return false;
    }
}