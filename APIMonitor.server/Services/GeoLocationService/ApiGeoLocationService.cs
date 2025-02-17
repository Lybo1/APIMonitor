using System.Text.Json;
using APIMonitor.server.Models;

namespace APIMonitor.server.Services.GeoLocationService;

public class ApiGeoLocationService : IGeoLocationService
{
    private readonly HttpClient httpClient;
    private const string ApiKey = "405bf98a5dbb63";

    public ApiGeoLocationService(HttpClient httpClient)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<IpGeolocation> GetLocationAsync(string ipAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress, "Invalid IPv4 address.");
        
        string requestUrl = $"https://ipinfo.io/{ipAddress}/json?token={ApiKey}";
        
        HttpResponseMessage? response = await httpClient.GetAsync(requestUrl);

        if (!response.IsSuccessStatusCode)
        {
            return new IpGeolocation();
        }
        
        string responseContent = await response.Content.ReadAsStringAsync();

        IpGeolocation? result = JsonSerializer.Deserialize<IpGeolocation>(responseContent);
        
        return result ?? new IpGeolocation();
    }
}