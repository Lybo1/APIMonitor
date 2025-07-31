using System.Security.Claims;
using APIMonitor.server.Models;
using APIMonitor.server.Services.IpBlockService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIMonitor.server.Identity.AdminControllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class IpBlockAdminController : ControllerBase
{
    private readonly IIpBlockService ipBlockService;

    public IpBlockAdminController(IIpBlockService ipBlockService)
    {
        this.ipBlockService = ipBlockService ?? throw new ArgumentNullException(nameof(ipBlockService));
    }

    [HttpGet]
    public async Task<IActionResult> GetAllBannedIps()
    {
        List<IpBlock> bannedIps = await ipBlockService.GetAllBannedIpsAsync();
        
        return Ok(bannedIps);
    }

    [HttpGet("{ipAddress}")]
    public async Task<IActionResult> GetBannedIp(string ipAddress)
    {
        IpBlock? ipBlock = await ipBlockService.GetBannedIpAsync(ipAddress);

        if (ipBlock is null)
        {
            return NotFound(new { message = "Ip address is not banned." });
        }
        
        return Ok(ipBlock);
    }

    [HttpPost("block")]
    public async Task<IActionResult> BlockIp([FromBody] BlockIpRequest request)
    {
        TimeSpan duration = TimeSpan.FromHours(request.DurationHours);
        
        int adminUserId = GetAdminUserId();
        bool result = await ipBlockService.BlockIpAsync(request.IpAddress, duration, request.Reason, adminUserId);

        if (!result)
        {
            return BadRequest(new { message = "Failed to block the IP address." });
        }
        
        return Ok(new { message = $"IP {request.IpAddress} has been blocked for {request.DurationHours} hours." });
    }

    [HttpDelete("unblock/{ipAddress}")]
    public async Task<IActionResult> UnblockIp(string ipAddress)
    {
        int adminUserId = GetAdminUserId();
        bool result = await ipBlockService.UnblockIpAsync(ipAddress, adminUserId);

        if (!result)
        {
            return NotFound(new { message = "IP address not found in block list." });
        }
        
        return Ok(new { message = $"IP {ipAddress} has been unblocked." });
    }

    private int GetAdminUserId()
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return string.IsNullOrEmpty(userId) ? throw new UnauthorizedAccessException("Invalid admin authentication.") : int.Parse(userId);
    }
}