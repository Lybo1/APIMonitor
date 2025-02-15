namespace APIMonitor.server.Services.SecurityService;

public interface ISecurityEventService
{
    event Action<string, string, string>? SuspiciousLoginAttempt;
    void TriggerSuspiciousLogin(string userId, string ipAddress, string userAgent);
}