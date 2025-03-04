using System.Diagnostics;
using APIMonitor.server.Data;
using APIMonitor.server.Hubs;
using APIMonitor.server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace APIMonitor.server.Services.ApiScannerService;

public class ApiScannerService : IApiScannerService
{
    private readonly HttpClient httpClient;
    private readonly ApplicationDbContext dbContext;
    private readonly IMemoryCache memoryCache;
    private readonly ILogger<ApiScannerService> logger;
    private readonly IHubContext<NotificationHub> hubContext;

    private static readonly AsyncRetryPolicy<HttpResponseMessage> retryPolicy = Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (response, timeSpan, retryCount, context) =>
            {
                if (retryCount == 3) // Only log the final failure
                {
                    Console.WriteLine($"❌ Final retry failed for {context["apiUrl"]}: {response.Result.StatusCode}");
                }
            });

    private static readonly AsyncTimeoutPolicy<HttpResponseMessage> timeoutPolicy = Policy
        .TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(5));

    private static readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> circuitBreakerPolicy = Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(20),
            onBreak: (response, breakDelay) =>
            {
                Console.WriteLine($"🚨 Circuit breaker OPEN for {breakDelay.TotalSeconds} seconds.");
            },
            onReset: () =>
            {
                Console.WriteLine("✅ Circuit breaker RESET.");
            },
            onHalfOpen: () =>
            {
                Console.WriteLine("⚠️ Circuit breaker HALF-OPEN: Testing API.");
            });

    public ApiScannerService(
        HttpClient httpClient,
        ApplicationDbContext dbContext,
        IMemoryCache memoryCache,
        ILogger<ApiScannerService> logger,
        IHubContext<NotificationHub> hubContext)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public async Task ScanApisAsync()
    {
        var apiUrls = await dbContext.ApiEndpoints
            .Where(api => api.IsActive)
            .Select(api => api.Url)
            .ToListAsync();

        if (!apiUrls.Any())
        {
            logger.LogWarning("⚠️ No active APIs found to scan.");
            await hubContext.Clients.All.SendAsync("ReceiveNotification", "No active APIs to scan.");
            return;
        }

        logger.LogInformation($"🚀 Scanning {apiUrls.Count} APIs...");
        await hubContext.Clients.All.SendAsync("ReceiveNotification", $"Scanning {apiUrls.Count} APIs...");

        int failedCount = 0;
        List<Task<ApiMetrics>> scanTasks = apiUrls
            .Where(apiUrl => !memoryCache.TryGetValue(apiUrl, out _))
            .Select(async apiUrl =>
            {
                var metrics = await ScanSingleApiAsync(apiUrl);
                if (metrics.ErrorsCount > 0) Interlocked.Increment(ref failedCount);
                return metrics;
            })
            .ToList();

        await Task.WhenAll(scanTasks);

        if (failedCount > 0)
        {
            logger.LogError($"❌ {failedCount} APIs failed.");
            await hubContext.Clients.All.SendAsync("ReceiveNotification", $"{failedCount} APIs failed.");
        }
        else
        {
            logger.LogInformation("✅ All APIs scanned successfully.");
            await hubContext.Clients.All.SendAsync("ReceiveNotification", "All APIs scanned successfully.");
        }
    }

    public async Task<ApiMetrics> ScanSingleApiAsync(string apiUrl, string? method = "GET", string? apiKey = null)
{
    if (string.IsNullOrWhiteSpace(apiUrl))
        throw new ArgumentException("API URL cannot be empty", nameof(apiUrl));

    if (memoryCache.TryGetValue(apiUrl, out ApiMetrics cachedMetrics))
    {
        logger.LogInformation($"📌 Returning cached metrics for {apiUrl}.");
        return cachedMetrics;
    }

    Stopwatch stopwatch = Stopwatch.StartNew();
    HttpResponseMessage response = null;
    bool success = false;
    string responseBody = string.Empty;
    string responseHeaders = string.Empty;

    try
    {
        var request = new HttpRequestMessage(new HttpMethod(method ?? "GET"), apiUrl);
        if (!string.IsNullOrEmpty(apiKey))
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

        response = await circuitBreakerPolicy.ExecuteAsync(() =>
            retryPolicy.ExecuteAsync(() =>
                timeoutPolicy.ExecuteAsync(() => httpClient.SendAsync(request))));

        stopwatch.Stop();
        responseBody = await response.Content.ReadAsStringAsync();
        responseHeaders = string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));
        success = response.IsSuccessStatusCode;
    }
    catch (Exception e)
    {
        stopwatch.Stop();
        logger.LogError($"❌ Error scanning {apiUrl}: {e.Message}");
    }

    var apiMetrics = new ApiMetrics
    {
        TotalRequests = 1,
        RequestsPerMinute = 1,
        AverageResponseTime = stopwatch.Elapsed,
        ErrorsCount = success ? 0 : 1,
    };

    memoryCache.Set(apiUrl, apiMetrics, TimeSpan.FromMinutes(5));
    return apiMetrics;
}
}
