using APIMonitor.server.Hubs;
using APIMonitor.server.Models;
using APIMonitor.server.Services.ApiScannerService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace APIMonitor.server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApiScanController : ControllerBase
{
    private readonly IApiScannerService apiScannerService;
    private readonly IHubContext<NotificationHub> hubContext;
    private readonly ILogger<ApiScannerController> logger;

    public ApiScanController(IApiScannerService apiScannerService, IHubContext<NotificationHub> hubContext, ILogger<ApiScannerController> logger)
    {
        this.apiScannerService = apiScannerService ?? throw new ArgumentNullException(nameof(apiScannerService));
        this.hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("scan")]
    public async Task<IActionResult> ScanApis([FromHeader]string id)
    {
        await apiScannerService.ScanApisAsync();

        await hubContext.Clients.User(id).SendAsync("ReceiveNotification", "API scan triggered!");
        
        return Ok(new { message = "Manual API scan triggered successfully!" });
    }
    
    [Authorize] 
    [HttpPost("scan-single")]
    public async Task<IActionResult> ScanSingleApi([FromQuery] string apiUrl)
    {
        ApiMetrics result = await apiScannerService.ScanSingleApiAsync(apiUrl);
        
        return Ok(result);
    }
}