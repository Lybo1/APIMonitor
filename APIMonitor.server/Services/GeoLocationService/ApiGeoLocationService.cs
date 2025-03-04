using MaxMind.GeoIP2; // For CityReader
using System.Net; // For IPAddress
using APIMonitor.server.Models; // For your AuditLog model (assumed)
using MaxMind.GeoIP2.Responses; // For CityResponse

namespace APIMonitor.server.Services.GeoLocationService;

public class ApiGeoLocationService : IGeoLocationService
{
    private readonly string databasePath;

    public ApiGeoLocationService(string path)
    {
        this.databasePath = path ?? throw new ArgumentNullException(nameof(path));
    }

    public (string country, string city, double? latitude, double? longitude, string timeZone) GetGeolocation(string ipAddress)
    {
        try
        {
            using (var reader = new City(databasePath)) // Use CityReader, not CityResponse
            {
                var response = reader.City(IPAddress.Parse(ipAddress)); // Get CityResponse from CityReader
                return (
                    response.Country.Name ?? "Unknown",
                    response.City.Name ?? "Unknown",
                    response.Location.Latitude,
                    response.Location.Longitude,
                    response.Location.TimeZone ?? "Unknown"
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Geolocation error for IP {ipAddress}: {ex.Message}");
            return ("Unknown", "Unknown", null, null, "Unknown");
        }
    }
}