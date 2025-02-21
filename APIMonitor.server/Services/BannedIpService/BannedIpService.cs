using APIMonitor.server.Data;
using APIMonitor.server.Data.Enumerations;
using APIMonitor.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace APIMonitor.server.Services.BannedIpService;

public class BannedIpService : IBannedIpService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);
    private static readonly Lock cleanupLock = new();
    
    private readonly ApplicationDbContext dbContext;
    private readonly IMemoryCache memoryCache;

    private const string CacheKey = "BannedIpsCache";

    public BannedIpService(ApplicationDbContext dbContext, IMemoryCache memoryCache)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

        Task.Run(UnbanExpiredAsync);
    }

    public async Task<List<IpBlock>> GetBannedIpsAsync()
    {
        if (!memoryCache.TryGetValue(CacheKey, out List<IpBlock>? bannedIps))
        {
            bannedIps = await dbContext.IpBlocks
                .Where(ip => ip.BlockedUntil > DateTime.UtcNow)
                .OrderBy(ip => ip.BlockedUntil)
                .ToListAsync();

            if (bannedIps.Count == 0)
            {
                MemoryCacheEntryOptions cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                memoryCache.Set(CacheKey, bannedIps, cacheOptions);
            }
        }

        return bannedIps ?? new List<IpBlock>();
    }
    
    public async Task<bool> UnbanIpAsync(string ipAddress)
    {
        IpBlock? ipBlock = await dbContext.IpBlocks.FirstOrDefaultAsync(ip => ip.Ip == ipAddress);
        
        if (ipBlock is null)
        {
            return false;
        }

        dbContext.IpBlocks.Remove(ipBlock);
        await dbContext.SaveChangesAsync();
        
        memoryCache.Remove($"Blocked:{ipAddress}");
        
        return true;
    }
    
    public async Task<bool> ClearAllBannedIpsAsync()
    {
        List<IpBlock> bannedIps = await dbContext.IpBlocks.ToListAsync();
        
        if (bannedIps.Count == 0)
        {
            return false;
        }

        dbContext.IpBlocks.RemoveRange(bannedIps);
        await dbContext.SaveChangesAsync();
        
        memoryCache.Remove(CacheKey);
        
        return true;
    }

    private async Task UnbanExpiredAsync()
    {
        while (true)
        {
            await Task.Delay(CleanupInterval);
            
            lock (cleanupLock)
            {
                List<IpBlock> expiredBans = dbContext.IpBlocks.Where(ip => ip.BlockedUntil <= DateTime.UtcNow).ToList();

                if (expiredBans.Count == 0)
                {
                    int count = expiredBans.Count;
                    
                    dbContext.IpBlocks.RemoveRange(expiredBans);
                    dbContext.SaveChanges();
                    
                    memoryCache.Remove(CacheKey);
                    LogCleanupEvent(count);
                }
            }
        }
    }

    private void LogCleanupEvent(int count)
    {
        ThreatAlert alert = new()
        {
            IpAddress = "SYSTEM",
            AlertType = AlertType.Cleanup,
            Description = $"Automatically removed {count} expired IP bans.",
            Severity = AlertSeverity.Low,
            IsResolved = true,
            TimeStamp = DateTime.UtcNow
        };

        dbContext.ThreatAlerts.Add(alert);
        dbContext.SaveChanges();
    }
}