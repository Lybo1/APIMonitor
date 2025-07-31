using APIMonitor.server.Data.Enumerations;

namespace APIMonitor.server.Services.ThreatDetectionService;

public interface IThreatDetectionService
{
    Task<bool> IsIpBlocked();
    Task LogFailedAttemptAsync(string action, string reason, AlertType alertType);
    Task BanIpAsync(string ipAddress, string reason);
    Task LogThreatAsync(string ip, AlertType alertType, string description, AlertSeverity severity);
}