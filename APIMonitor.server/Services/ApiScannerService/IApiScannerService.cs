using APIMonitor.server.Models;

namespace APIMonitor.server.Services.ApiScannerService;

public interface IApiScannerService
{
    Task ScanApisAsync();
    Task<ApiMetrics> ScanSingleApiAsync(string apiUrl);
}