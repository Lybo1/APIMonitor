using System.Net;
using APIMonitor.server.Models;

namespace APIMonitor.server.Services.ApiScannerService;

public interface IApiScannerService
{
    Task<(ApiMetrics Metrics, HttpStatusCode StatusCode, string ResponseSnippet)> ScanSingleApiAsync(string apiUrl, string? method = "GET", string? apiKey = null, string? userId = null, bool forceRefresh = false);
}