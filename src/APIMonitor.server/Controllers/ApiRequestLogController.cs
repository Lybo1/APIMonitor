using APIMonitor.server.Data;
using APIMonitor.server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIMonitor.server.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class ApiRequestLogController : ControllerBase
{
    private readonly ApplicationDbContext dbContext;

    public ApiRequestLogController(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? statusCode,
        [FromQuery] string? ipAddress,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1)
        {
            return BadRequest(new { message = "Page and pageSize must be positive numbers." });
        }

        IQueryable<ApiRequestLog> query = dbContext.ApiRequestLogs.AsQueryable();
        
        if (startDate.HasValue)
        {
            query = query.Where(log => log.TimeStamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(log => log.TimeStamp <= endDate.Value);
        }

        if (statusCode.HasValue)
        {
            query = query.Where(log => log.StatusCode == statusCode.Value);
        }

        if (!string.IsNullOrEmpty(ipAddress))
        {
            query = query.Where(log => log.IpAddress == ipAddress);
        }
        
        int totalRecords = await query.CountAsync();
        List<ApiRequestLog> logs = await query
            .OrderByDescending(log => log.TimeStamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        if (logs.Count == 0)
        {
            return NotFound(new { message = "No API request logs found matching the criteria." });
        }

        return Ok(new
        {
            totalRecords,
            currentPage = page,
            totalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            logs
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLogById(int id)
    {
        ApiRequestLog? log = await dbContext.ApiRequestLogs.FindAsync(id);

        if (log == null)
        {
            return NotFound(new { message = $"No log entry found with ID {id}." });
        }

        return Ok(log);
    }

    [HttpDelete("cleanup")]
    public async Task<IActionResult> DeleteOldLogs([FromQuery] int days = 30)
    {
        if (days < 1)
        {
            return BadRequest(new { message = "Days must be a positive number." });
        }

        DateTime cutoffDate = DateTime.UtcNow.AddDays(-days);
        List<ApiRequestLog> oldLogs = await dbContext.ApiRequestLogs.Where(log => log.TimeStamp < cutoffDate).ToListAsync();

        if (oldLogs.Count == 0)
        {
            return NotFound(new { message = $"No logs older than {days} days found." }); 
        }
        
        dbContext.ApiRequestLogs.RemoveRange(oldLogs);
        await dbContext.SaveChangesAsync();

        return Ok(new { message = $"{oldLogs.Count} old logs deleted successfully." });
    }
}