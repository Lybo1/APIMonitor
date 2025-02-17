namespace APIMonitor.server.Services.ThreatDetectionService;

public interface IThreatDetectionService
{
    Task LogFailedAttemptAsync(string reason);
    Task<bool> IsIpBlocked();
}