using APIMonitor.server.Models;
using APIMonitor.server.Services.AuditLogService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace APIMonitor.server.Identity.AdminControllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("adminPolicy")]
public class AuditLogAdminController : ControllerBase
{
    private readonly IAuditLogService auditLogService;

    public AuditLogAdminController(IAuditLogService auditLogService)
    {
        this.auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAuditLogs([FromHeader(Name = "Authorization")] string token)
    {
        List<AuditLog> logs = await auditLogService.GetAllAuditLogsAsync();
        
        return Ok(logs);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAuditLogById([FromRoute] int id, [FromHeader(Name = "Authorization")] string token)
    {
        AuditLog? log = await auditLogService.GetAuditLogByIdAsync(id);
        
        return log is not null ? Ok(log) : NotFound(new { message = "Log not found." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAuditLog([FromRoute] int id, [FromHeader(Name = "Authorization")] string token)
    {
        bool deleted = await auditLogService.DeleteAuditLogAsync(id);
        
        return deleted ? Ok(new { message = "Log deleted successfully." }) : NotFound(new { message = "Log not found." });
    }
    
    [HttpDelete("purge")]
    public async Task<IActionResult> PurgeAuditLogs([FromHeader(Name = "Authorization")] string token)
    {
        await auditLogService.PurgeAuditLogsAsync();
        
        return Ok(new { message = "All audit logs deleted." });
    }
}