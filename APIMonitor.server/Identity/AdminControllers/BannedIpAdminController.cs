using System.ComponentModel.DataAnnotations;
using APIMonitor.server.Data;
using APIMonitor.server.Data.Enumerations;
using APIMonitor.server.Models;
using APIMonitor.server.Services.BannedIpService;
using APIMonitor.server.Services.ThreatDetectionService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace APIMonitor.server.Identity.AdminControllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class BannedIpAdminController : ControllerBase
{
    private readonly IBannedIpService bannedIpService;
    private readonly IThreatDetectionService threatDetectionService;
    private readonly IMemoryCache memoryCache;
    
    private const string CacheKey = "BannedIpsCache";

    public BannedIpAdminController(IMemoryCache memoryCache, IThreatDetectionService threatDetectionService, IBannedIpService bannedIpService)
    {
        this.bannedIpService = bannedIpService ?? throw new ArgumentNullException(nameof(bannedIpService));
        this.threatDetectionService = threatDetectionService ?? throw new ArgumentNullException(nameof(threatDetectionService));
        this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    [HttpGet]
    public async Task<IActionResult> GetBannedIps()
    {
        if (memoryCache.TryGetValue(CacheKey, out List<IpBlock>? bannedIps))
        {
            return Ok(bannedIps);
        }
        
        bannedIps = await bannedIpService.GetBannedIpsAsync();
            
        if (bannedIps.Count == 0)
        {
            return NotFound(new { message = "No currently banned IPs." });
        }

        MemoryCacheEntryOptions cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            
        memoryCache.Set(CacheKey, bannedIps, cacheOptions);

        return Ok(bannedIps);
    }

    [HttpDelete("{ipAddress}")]
    public async Task<IActionResult> UnbanIp([FromRoute, Required, StringLength(15)] string ipAddress) 
    {
        bool unbanned = await bannedIpService.UnbanIpAsync(ipAddress);
        
        if (!unbanned)
        {
            return NotFound(new { message = $"IP {ipAddress} not found or already unbanned." });
        }

        memoryCache.Remove(CacheKey);

        await threatDetectionService.LogThreatAsync(ipAddress, AlertType.IpBanned, $"Admin unbanned IP {ipAddress}", AlertSeverity.Low);

        return Ok(new { message = $"IP {ipAddress} has been successfully unbanned." });
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearAllBans()
    {
        bool cleared = await bannedIpService.ClearAllBannedIpsAsync();
        
        if (!cleared)
        {
            return NotFound(new { message = "No banned IPs to clear." });
        }
        
        memoryCache.Remove(CacheKey);

        await threatDetectionService.LogThreatAsync("ALL", AlertType.IpBanned, "Admin cleared all IP bans.", AlertSeverity.Low);
        
        return Ok(new { message = "All banned IPs have been successfully cleared." });
    }
}