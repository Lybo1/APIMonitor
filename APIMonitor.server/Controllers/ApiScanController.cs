using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using APIMonitor.server.Models;
using APIMonitor.server.Services.ApiScannerService;
using APIMonitor.server.ViewModels;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApiScannerController : ControllerBase
{
    private readonly IApiScannerService scannerService;
    private readonly ILogger<ApiScannerController> logger;

    public ApiScannerController(IApiScannerService scannerService, ILogger<ApiScannerController> logger)
    {
        this.scannerService = scannerService;
        this.logger = logger;
    }

    [HttpPost("scan")]
    public async Task<ActionResult<ScanResponse>> ScanApi([FromBody] ScanRequest request)
    {
        try
        {
            logger.LogInformation($"Scan request for {request.ApiUrl} with method {request.Method}");

            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            (ApiMetrics metrics, HttpStatusCode statusCode, string responseSnippet) = await scannerService.ScanSingleApiAsync(
                request.ApiUrl,
                request.Method,
                request.ApiKey,
                userId,
                request.ForceRefresh);

            return Ok(new ScanResponse
            {
                Metrics = metrics,
                StatusCode = statusCode,
                ResponseSnippet = responseSnippet
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during API scan");
            return StatusCode(500, "An unexpected error occurred");
        }
    }
}