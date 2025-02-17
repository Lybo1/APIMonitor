using APIMonitor.server.Data;
using APIMonitor.server.Models;
using APIMonitor.server.Services.AuditLogService;
using Microsoft.EntityFrameworkCore;

namespace APIMonitor.server.Services.IpBlockService;

public class IpBlockService : IIpBlockService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IAuditLogService auditLogService;

    public IpBlockService(ApplicationDbContext dbContext, IAuditLogService auditLogService)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }
    
    public async Task<List<IpBlock>> GetAllBannedIpsAsync()
    {
        return await dbContext.IpBlocks
            .Where(ip => ip.BlockedUntil > DateTime.UtcNow)
            .OrderByDescending(ip => ip.BlockedUntil)
            .ToListAsync();
    }

    public async Task<IpBlock?> GetBannedIpAsync(string ipAddress)
    {
        return await dbContext.IpBlocks.FirstOrDefaultAsync(ip => ip.Ip == ipAddress);
    }

    public async Task<bool> UnblockIpAsync(string ipAddress, int adminUserId)
    {
        IpBlock? ipBlock = await dbContext.IpBlocks.FirstOrDefaultAsync(ip => ip.Ip == ipAddress);
        
        if (ipBlock is null)
        {
            return false;
        }
        
        dbContext.IpBlocks.Remove(ipBlock);
        await dbContext.SaveChangesAsync();

        await auditLogService.LogActionAsync(
            adminUserId,
            "Unblocked IP",
            $"Admin unblocked IP {ipAddress}",
            DateTime.UtcNow);
        
        return true;
    }

    public async Task<bool> BlockIpAsync(string ipAddress, TimeSpan duration, string reason, int adminUserId)
    {
        IpBlock? exisitngBan = await dbContext.IpBlocks.FirstOrDefaultAsync(ip => ip.Ip == ipAddress);

        if (exisitngBan is not null)
        {
            exisitngBan.BlockedUntil = DateTime.UtcNow.Add(duration);
            exisitngBan.Reason = reason;
        }
        else
        {
            IpBlock newBlock = new()
            {
                Ip = ipAddress,
                BlockedUntil = DateTime.UtcNow.Add(duration),
                FailedAttempts = 0,
                Reason = reason
            };
            
            await dbContext.IpBlocks.AddAsync(newBlock);
        }
        
        await dbContext.SaveChangesAsync();

        await auditLogService.LogActionAsync(
            adminUserId,
            "Blocked IP",
            $"Admin blocked IP {ipAddress} for {duration.TotalHours} hours. Reason: {reason}",
            DateTime.UtcNow);
        
        return true;
    }
}