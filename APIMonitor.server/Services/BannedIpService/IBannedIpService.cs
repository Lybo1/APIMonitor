using APIMonitor.server.Models;

namespace APIMonitor.server.Services.BannedIpService;

public interface IBannedIpService
{
    Task<List<IpBlock>> GetBannedIpsAsync();
    Task<bool> UnbanIpAsync(string ipAddress);
    Task<bool> ClearAllBannedIpsAsync();
}