using APIMonitor.server.Data;
using APIMonitor.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace APIMonitor.server.Services.BannedIpService;

public class BannedIpService : IBannedIpService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IMemoryCache memoryCache;

    public BannedIpService(ApplicationDbContext dbContext, IMemoryCache memoryCache)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    public async Task<List<IpBlock>> GetBannedIpsAsync()
    {
        return await dbContext.IpBlocks
            .Where(ip => ip.BlockedUntil > DateTime.UtcNow)
            .OrderBy(ip => ip.BlockedUntil)
            .ToListAsync();
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
        
        foreach (IpBlock ip in bannedIps)
        {
            memoryCache.Remove($"Blocked:{ip.Ip}");
        }

        await dbContext.SaveChangesAsync();
        
        return true;
    }
}