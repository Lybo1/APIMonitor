using APIMonitor.server.Services.ApiScannerService;
using Microsoft.AspNetCore.Mvc;

namespace APIMonitor.server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApiScannerController : ControllerBase
{
    private readonly IApiScannerService apiScannerService;
    private readonly ILogger<ApiScannerController> logger;
    private static Timer? scheduledScanTimer;

    public ApiScannerController(IApiScannerService apiScannerService, ILogger<ApiScannerController> logger)
    {
        this.apiScannerService = apiScannerService ?? throw new ArgumentNullException(nameof(apiScannerService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("scan")]
    public async Task<IActionResult> TriggerApiScan()
    {
        await apiScannerService.ScanApisAsync();
        
        return Ok(new { message = "🚀 Manual API scan triggered successfully!" });
    }

    [HttpPost("schedule")]
    public IActionResult ScheduleApiScan([FromQuery] int intervalMinutes)
    {
        if (intervalMinutes < 1)
        {
            return BadRequest(new { message = "❌ Interval must be at least 1 minute." });
        }
        
        scheduledScanTimer?.Dispose();
        
        scheduledScanTimer = new Timer(async _ =>
        {
            logger.LogInformation($"⏳ Scheduled API scan started at {DateTime.UtcNow}");
            await apiScannerService.ScanApisAsync();
        }, null, TimeSpan.Zero, TimeSpan.FromMinutes(intervalMinutes));

        return Ok(new { message = $"✅ API scans scheduled every {intervalMinutes} minutes!" });
    }

    [HttpPost("stop-schedule")]
    public IActionResult StopScheduledScan()
    {
        scheduledScanTimer?.Dispose();
        
        return Ok(new { message = "⛔ Scheduled API scans stopped." });
    }
}