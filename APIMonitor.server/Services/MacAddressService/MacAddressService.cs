using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace APIMonitor.server.Services.MacAddressService;

public class MacAddressService : IMacAddressService
{
    public async Task<string?> GetMacAddressAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        
        string ipAddress = context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? string.Empty;
        
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return null;
        }

        string? macAddress = await Task.Run(() =>
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
                .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork && addr.Address.ToString() == ipAddress )
                .Select(addr => addr.Address.ToString())
                .FirstOrDefault();
        });
        
        return macAddress;
    }
}