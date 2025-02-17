using APIMonitor.server.Models;

namespace APIMonitor.server.Services.IpBlockService;

public interface IIpBlockService
{
    Task<List<IpBlock>> GetAllBannedIpsAsync();
    Task<IpBlock?> GetBannedIpAsync(string ipAddress);
    Task<bool> UnblockIpAsync(string ipAddress, int adminUserId);
    Task<bool> BlockIpAsync(string ipAddress, TimeSpan duration, string reason, int adminUserId);
}