namespace APIMonitor.server.Services.MacAddressService;

public interface IMacAddressService
{
    Task<string?> GetMacAddressAsync(HttpContext context);
}