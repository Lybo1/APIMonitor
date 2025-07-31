using APIMonitor.server.Data;
using APIMonitor.server.Data.Enumerations;
using APIMonitor.server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIMonitor.server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ThreatLogsController : ControllerBase
{
    private readonly ApplicationDbContext dbContext;

    public ThreatLogsController(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet]
    public async Task<IActionResult> GetThreatLogs([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] AlertSeverity? severity, [FromQuery] string? adminName)
    {
        IQueryable<ThreatAlert> query = dbContext.ThreatAlerts.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(alert => alert.TimeStamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(alert => alert.TimeStamp <= endDate.Value);
        }

        if (severity.HasValue)
        {
            query = query.Where(alert => alert.Severity == severity.Value);
        }

        if (!string.IsNullOrEmpty(adminName))
        {
            query = query.Where(alert => alert.Description.Contains(adminName));
        }

        List<ThreatAlert> logs = await query.OrderByDescending(alert => alert.TimeStamp).ToListAsync();
        
        return logs.Any() ? Ok(logs) : NotFound(new { message = "No matching threat logs found." });
    }
}