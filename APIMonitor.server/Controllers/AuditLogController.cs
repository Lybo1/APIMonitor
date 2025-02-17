using System.Security.Claims;
using APIMonitor.server.Data;
using APIMonitor.server.Models;
using APIMonitor.server.Services.AuditLogService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIMonitor.server.Controllers;

[Authorize(Roles = "User")]
[ApiController]
[Route("api/[controller]")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
    {
        this.auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    [HttpGet("user-logs")]
    public async Task<IActionResult> GetUserAuditLogs([FromHeader(Name = "Authorization")] string token)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid authentication." });
        }
        
        List<AuditLog> logs = await auditLogService.GetUserAuditLogsAsync(Convert.ToInt32(userId));
        
        return Ok(logs);
        
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAuditLogById([FromRoute] int id, [FromHeader(Name = "Authorization")] string token)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid authentication." });
        }
        
        AuditLog? log = await auditLogService.GetAuditLogByIdAsync(id, Convert.ToInt32(userId));
        
        return log is not null ? Ok(log) : NotFound(new { message = "Log not found." });
    }
}