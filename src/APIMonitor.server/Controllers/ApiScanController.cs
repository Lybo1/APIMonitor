using System.Net;
using System.Security.Claims;
using APIMonitor.server.Models;
using APIMonitor.server.Services.ApiScannerService;
using APIMonitor.server.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIMonitor.server.Controllers
{

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

                (ApiMetrics metrics, HttpStatusCode statusCode, string responseSnippet) =
                    await scannerService.ScanSingleApiAsync(
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

        [HttpPost("scan-single")]
        public async Task<ActionResult<ScanResponse>> ScanSingleApi([FromQuery] string apiUrl, [FromQuery] string method = "GET", [FromQuery] string? apiKey = null, [FromQuery] bool forceRefresh = false)
        {
            try
            {
                logger.LogInformation($"Received single scan request: apiUrl={apiUrl}, method={method}, apiKey={apiKey ?? "null"}, forceRefresh={forceRefresh}, UserId={User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "null"}");

                string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                (ApiMetrics metrics, HttpStatusCode statusCode, string responseSnippet) = await scannerService.ScanSingleApiAsync(
                    apiUrl,
                    method,
                    apiKey,
                    userId,
                    forceRefresh);

                logger.LogInformation($"Scan completed: StatusCode={statusCode}, ResponseSnippet={responseSnippet}");

                return Ok(new ScanResponse
                {
                    Metrics = metrics,
                    StatusCode = statusCode,
                    ResponseSnippet = responseSnippet
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during single API scan");
                return StatusCode(500, "An unexpected error occurred");
            }
        }
    }
}