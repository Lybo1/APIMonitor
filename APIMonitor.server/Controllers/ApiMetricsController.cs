using APIMonitor.server.Data;
using APIMonitor.server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIMonitor.server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApiMetricsController : ControllerBase
{
    private readonly ApplicationDbContext dbContext;

    public ApiMetricsController(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }
    
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetMetrics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1)
        {
            return BadRequest(new { message = "Page and pageSize must be positive numbers." });
        }

        IQueryable<ApiMetrics> query = dbContext.ApiMetrics.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(metric => metric.TimeStamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(metric => metric.TimeStamp <= endDate.Value);
        }

        int totalRecords = await query.CountAsync();
        
        List<ApiMetrics> metrics = await query
            .OrderByDescending(metric => metric.TimeStamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        if (metrics.Count == 0)
        {
            return NotFound(new { message = "No API metrics found matching the criteria." });
        }

        return Ok(new
        {
            totalRecords,
            currentPage = page,
            totalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            metrics
        });
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetMetricsById(int id)
    {
        ApiMetrics? metric = await dbContext.ApiMetrics.FindAsync(id);
        
        if (metric == null)
        {
            return NotFound(new { message = $"No metric entry found with ID {id}." });
        }

        return Ok(metric);
    }

    [Authorize]
    [HttpGet("summary")]
    public async Task<IActionResult> GetMetricsSummary([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        IQueryable<ApiMetrics> query = dbContext.ApiMetrics.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(metric => metric.TimeStamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(metric => metric.TimeStamp <= endDate.Value);
        }
        
        List<ApiMetrics> metrics = await query.ToListAsync();
        
        var summary = new
        {
            totalRequests = metrics.Sum(m => m.TotalRequests),
            averageResponseTimeMs = metrics.Any() ? metrics.Average(m => m.AverageResponseTime.TotalMilliseconds) : 0,
            totalErrors = metrics.Sum(m => m.ErrorsCount)
        };
        
        return Ok(summary);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("cleanup")]
    public async Task<IActionResult> DeleteOldMetrics([FromQuery] int days = 30)
    {
        if (days < 1)
        {
            return BadRequest(new { message = "Days must be a positive number." });
        }

        DateTime cutoffDate = DateTime.UtcNow.AddDays(-days);
        List<ApiMetrics> oldMetrics = await dbContext.ApiMetrics.Where(metric => metric.TimeStamp < cutoffDate).ToListAsync();
        
        if (oldMetrics.Count == 0)
        {
            return NotFound(new { message = $"No metrics older than {days} days found." });
        }

        dbContext.ApiMetrics.RemoveRange(oldMetrics);
        await dbContext.SaveChangesAsync();

        return Ok(new { message = $"{oldMetrics.Count} old metrics deleted successfully." });
    }
}