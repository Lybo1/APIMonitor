using APIMonitor.server.Models;

namespace APIMonitor.server.Services.GeoLocationService;

public interface IGeoLocationService
{
    (string country, string city, double? latitude, double? longitude, string timeZone) GetGeolocation(string ipAddress);
}