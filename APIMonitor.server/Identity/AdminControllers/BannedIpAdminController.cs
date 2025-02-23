using System.ComponentModel.DataAnnotations;
using APIMonitor.server.Data;
using APIMonitor.server.Data.Enumerations;
using APIMonitor.server.Hubs;
using APIMonitor.server.Models;
using APIMonitor.server.Services.BannedIpService;
using APIMonitor.server.Services.ThreatDetectionService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
    private readonly IHubContext<NotificationHub> hubContext;
    
    public BannedIpAdminController(IThreatDetectionService threatDetectionService, IBannedIpService bannedIpService, IHubContext<NotificationHub> hubContext)
    {
        this.bannedIpService = bannedIpService ?? throw new ArgumentNullException(nameof(bannedIpService));
        this.threatDetectionService = threatDetectionService ?? throw new ArgumentNullException(nameof(threatDetectionService));
        this.hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetBannedIps()
    {
        List<IpBlock> bannedIps = await bannedIpService.GetBannedIpsAsync();

        if (bannedIps.Count == 0)
        {
            return NotFound(new { message = "No currently banned IPs." });
        }

        return Ok(bannedIps);
    }

    [HttpDelete("{ipAddress}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UnbanIp([FromRoute, Required, StringLength(15)] string ipAddress)
    {
        string adminName = User.Identity?.Name ?? "Unknown";
        
        bool unbanned = await bannedIpService.UnbanIpAsync(ipAddress);

        if (!unbanned)
        {
            return NotFound(new { message = $"IP {ipAddress} not found or already unbanned." });
        }

        await threatDetectionService.LogThreatAsync(ipAddress, AlertType.IpUnbanned, "Admin removed ban", AlertSeverity.Low);
        await hubContext.Clients.All.SendAsync("ReceiveNotification", $"✅ IP {ipAddress} has been unbanned!");

        return Ok(new { message = $"IP {ipAddress} has been successfully unbanned by {adminName}." });
    }

    [HttpDelete("clear")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ClearAllBans()
    {
        string adminName = User.Identity?.Name ?? "Unknown";
        
        bool cleared = await bannedIpService.ClearAllBannedIpsAsync();

        if (!cleared)
        {
            return NotFound(new { message = "No banned IPs to clear." });
        }

        await threatDetectionService.LogThreatAsync("ALL", AlertType.IpUnbanned, "Admin cleared all IP bans", AlertSeverity.Medium);

        return Ok(new { message = $"All banned IPs have been successfully cleared by {adminName}." });
    }
}