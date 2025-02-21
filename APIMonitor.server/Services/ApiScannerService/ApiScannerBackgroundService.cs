namespace APIMonitor.server.Services.ApiScannerService;

public class ApiScannerBackgroundService : BackgroundService
{
    private readonly IApiScannerService apiScannerService;

    public ApiScannerBackgroundService(IApiScannerService apiScannerService)
    {
        this.apiScannerService = apiScannerService ?? throw new ArgumentNullException(nameof(apiScannerService));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await apiScannerService.ScanApisAsync();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}