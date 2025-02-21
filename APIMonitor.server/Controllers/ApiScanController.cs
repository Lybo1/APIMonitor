using APIMonitor.server.Models;
using APIMonitor.server.Services.ApiScannerService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIMonitor.server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApiScanController : ControllerBase
{
    private readonly IApiScannerService apiScannerService;

    public ApiScanController(IApiScannerService apiScannerService)
    {
        this.apiScannerService = apiScannerService ?? throw new ArgumentNullException(nameof(apiScannerService));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("scan")]
    public async Task<IActionResult> ScanApis()
    {
        await apiScannerService.ScanApisAsync();
        
        return Ok(new { message = "API scan started successfully." });
    }
    
    [Authorize] 
    [HttpPost("scan-single")]
    public async Task<IActionResult> ScanSingleApi([FromQuery] string apiUrl)
    {
        ApiMetrics result = await apiScannerService.ScanSingleApiAsync(apiUrl);
        
        return Ok(result);
    }
}