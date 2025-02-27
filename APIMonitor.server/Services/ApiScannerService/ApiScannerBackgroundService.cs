namespace APIMonitor.server.Services.ApiScannerService;

public class ApiScannerBackgroundService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;

    public ApiScannerBackgroundService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                IApiScannerService apiScannerService = scope.ServiceProvider.GetRequiredService<IApiScannerService>();

                await apiScannerService.ScanApisAsync();
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}