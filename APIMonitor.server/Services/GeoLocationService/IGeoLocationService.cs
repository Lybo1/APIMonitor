using APIMonitor.server.Models;

namespace APIMonitor.server.Services.GeoLocationService;

public interface IGeoLocationService
{
    Task<IpGeolocation> GetLocationAsync(string ipAddress);
}