using System.Diagnostics;
using System.Net;
using System.Net.Http;
using APIMonitor.server.Data;
using APIMonitor.server.Hubs;
using APIMonitor.server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace APIMonitor.server.Services.ApiScannerService;

public class ApiScannerService : IApiScannerService
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ApiScannerService> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;

    private static readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            async (response, timeSpan, retryCount, context) =>
            {
                string apiUrl = context["apiUrl"]?.ToString() ?? "unknown";
                string userId = context["userId"]?.ToString();
                if (userId != null)
                {
                    await context["hubContext"]?.As<IHubContext<NotificationHub>>()
                        .Clients.User(userId)
                        .SendAsync("ReceiveNotification",
                            $"[{DateTime.UtcNow:O}] Retry {retryCount}/3 after {timeSpan.TotalSeconds}s for {apiUrl}: {(int)response.Result.StatusCode} {response.Result.StatusCode}");
                }
                if (retryCount == 3)
                {
                    Console.WriteLine($"❌ Final retry failed for {apiUrl}: {response.Result.StatusCode}");
                }
            });

    private static readonly AsyncTimeoutPolicy<HttpResponseMessage> _timeoutPolicy = Policy
        .TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(5), TimeoutStrategy.Pessimistic,
            async (context, timeSpan, task) =>
            {
                string userId = context["userId"]?.ToString();
                if (userId != null)
                {
                    await context["hubContext"]?.As<IHubContext<NotificationHub>>()
                        .Clients.User(userId)
                        .SendAsync("ReceiveNotification",
                            $"[{DateTime.UtcNow:O}] Timeout after {timeSpan.TotalSeconds}s for {context["apiUrl"]}");
                }
            });

    private static readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreakerPolicy = Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(20),
            onBreak: async (response, breakDelay, context) =>
            {
                string userId = context["userId"]?.ToString();
                if (userId != null)
                {
                    await context["hubContext"]?.As<IHubContext<NotificationHub>>()
                        .Clients.User(userId)
                        .SendAsync("ReceiveNotification",
                            $"[{DateTime.UtcNow:O}] Circuit breaker OPEN for {breakDelay.TotalSeconds}s due to repeated failures");
                }
                Console.WriteLine($"🚨 Circuit breaker OPEN for {breakDelay.TotalSeconds} seconds.");
            },
            onReset: async context =>
            {
                string userId = context["userId"]?.ToString();
                if (userId != null)
                {
                    await context["hubContext"]?.As<IHubContext<NotificationHub>>()
                        .Clients.User(userId)
                        .SendAsync("ReceiveNotification",
                            $"[{DateTime.UtcNow:O}] Circuit breaker RESET");
                }
                Console.WriteLine("✅ Circuit breaker RESET.");
            },
            onHalfOpen: async context =>
            {
                string userId = context["userId"]?.ToString();
                if (userId != null)
                {
                    await context["hubContext"]?.As<IHubContext<NotificationHub>>()
                        .Clients.User(userId)
                        .SendAsync("ReceiveNotification",
                            $"[{DateTime.UtcNow:O}] Circuit breaker HALF-OPEN: Testing API");
                }
                Console.WriteLine("⚠️ Circuit breaker HALF-OPEN: Testing API.");
            });

    public ApiScannerService(
        HttpClient httpClient,
        ApplicationDbContext dbContext,
        IMemoryCache memoryCache,
        ILogger<ApiScannerService> logger,
        IHubContext<NotificationHub> hubContext)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public async Task<(ApiMetrics Metrics, HttpStatusCode StatusCode, string ResponseSnippet)> ScanSingleApiAsync(
        string apiUrl, string? method = "GET", string? apiKey = null, string? userId = null, bool forceRefresh = false)
    {
        if (string.IsNullOrWhiteSpace(apiUrl))
            throw new ArgumentException("API URL cannot be empty", nameof(apiUrl));

        if (string.IsNullOrWhiteSpace(method))
            throw new ArgumentException("HTTP method cannot be empty", nameof(method));

        var context = new Context
        {
            ["apiUrl"] = apiUrl,
            ["userId"] = userId,
            ["hubContext"] = _hubContext
        };

        if (!forceRefresh && _memoryCache.TryGetValue(apiUrl, out ApiMetrics cachedMetrics))
        {
            _logger.LogInformation($"📌 Returning cached metrics for {apiUrl}.");
            if (userId != null)
            {
                await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification",
                    $"[{DateTime.UtcNow:O}] Using cached metrics for {apiUrl} ({method})");
                await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification",
                    $"[{DateTime.UtcNow:O}] Cached result - Total: {cachedMetrics.AverageResponseTime.TotalMilliseconds:F2}ms, Errors: {cachedMetrics.ErrorsCount}");
            }
            return (cachedMetrics, HttpStatusCode.OK, "Cached response");
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        HttpResponseMessage response = null;
        bool success = false;
        string responseBody = string.Empty;
        string responseHeaders = string.Empty;

        try
        {
            var request = new HttpRequestMessage(new HttpMethod(method), apiUrl);
            if (!string.IsNullOrEmpty(apiKey))
                request.Headers.Add("Authorization", $"Bearer {apiKey}");

            if (method == "POST" && apiUrl.EndsWith("/api/register/register"))
            {
                request.Content = new StringContent(
                    "{\"email\":\"testuser" + DateTime.UtcNow.Ticks + "@example.com\",\"password\":\"Test123!\",\"confirmPassword\":\"Test123!\",\"rememberMe\":true}",
                    System.Text.Encoding.UTF8,
                    "application/json"
                );
                if (userId != null)
                {
                    await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification",
                        $"[{DateTime.UtcNow:O}] Preparing POST body with registration data for {apiUrl}");
                }
            }
            else if (userId != null)
            {
                await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification",
                    $"[{DateTime.UtcNow:O}] Preparing {method} request (no body) for {apiUrl}");
            }

            // Step 1: Notify starting request
            if (userId != null)
            {
                await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification",
                    $"[{DateTime.UtcNow:O}] Starting HTTP {method} request to {apiUrl}");
            }

            response = await _circuitBreakerPolicy.ExecuteAsync(
                () => _retryPolicy.ExecuteAsync(
                    () => _timeoutPolicy.ExecuteAsync(async () =>
                    {
                        var resp = await _httpClient.SendAsync(request);
                        // Step 2: Notify response received
                        if (userId != null)
                        {
                            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification",
                                $"[{DateTime.UtcNow:O}] Received response: {(int)resp.StatusCode} {resp.StatusCode}");
                        }
                        return resp;
                    }, context), context), context);

            stopwatch.Stop();
            responseBody = await response.Content.ReadAsStringAsync();
            responseHeaders = string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));
            success = response.IsSuccessStatusCode;

            // Step 3: Notify response details
            if (userId != null)
            {
                await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification",
                    $"[{DateTime.UtcNow:O}] Response time: {stopwatch.ElapsedMilliseconds}ms, Success: {success}, Headers: {responseHeaders}");
                await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification",
                    $"[{DateTime.UtcNow:O}] Response body (first 100 chars): {responseBody.Length > 0 ? responseBody.Substring(0, Math.Min(100, responseBody.Length)) : "Empty"}");
            }
        }
        catch (Exception e)
        {
            stopwatch.Stop();
            _logger.LogError($"❌ Error scanning {apiUrl}: {e.Message}");
            if (userId != null)
            {
                await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification",
                    $"[{DateTime.UtcNow:O}] Scan error: {e.Message}");
            }
        }

        var apiMetrics = new ApiMetrics
        {
            TotalRequests = 1,
            RequestsPerMinute = 1,
            AverageResponseTime = stopwatch.Elapsed,
            ErrorsCount = success ? 0 : 1
        };

        _memoryCache.Set(apiUrl, apiMetrics, TimeSpan.FromMinutes(5));

        // Step 4: Notify caching
        if (userId != null)
        {
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification",
                $"[{DateTime.UtcNow:O}] Metrics cached for {apiUrl} ({method}) - Total: {apiMetrics.AverageResponseTime.TotalMilliseconds:F2}ms, Errors: {apiMetrics.ErrorsCount}");
        }

        string responseSnippet = responseBody.Length > 0 ? responseBody.Substring(0, Math.Min(100, responseBody.Length)) : "Empty";
        return (apiMetrics, response?.StatusCode ?? HttpStatusCode.RequestTimeout, responseSnippet);
    }

    public async Task ScanApisAsync()
    {
        var apiUrls = await _dbContext.ApiEndpoints
            .Where(api => api.IsActive)
            .Select(api => api.Url)
            .ToListAsync();

        if (!apiUrls.Any())
        {
            _logger.LogWarning("⚠️ No active APIs found to scan.");
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "No active APIs to scan.");
            return;
        }

        _logger.LogInformation($"🚀 Scanning {apiUrls.Count} APIs...");
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Scanning {apiUrls.Count} APIs...");

        int failedCount = 0;
        List<Task<ApiMetrics>> scanTasks = apiUrls
            .Where(apiUrl => !_memoryCache.TryGetValue(apiUrl, out _))
            .Select(async apiUrl =>
            {
                var (metrics, _, _) = await ScanSingleApiAsync(apiUrl);
                if (metrics.ErrorsCount > 0) Interlocked.Increment(ref failedCount);
                return metrics;
            })
            .ToList();

        await Task.WhenAll(scanTasks);

        if (failedCount > 0)
        {
            _logger.LogError($"❌ {failedCount} APIs failed.");
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"{failedCount} APIs failed.");
        }
        else
        {
            _logger.LogInformation("✅ All APIs scanned successfully.");
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "All APIs scanned successfully.");
        }
    }
}