namespace APIMonitor.server.Data.Enumerations;

public enum AlertType
{
    DDoS,
    RateLimitExceeded,
    ErrorSpike,
    UnauthorizedAccess,
    SuspiciousActivity,
    IpBanned,
    IpUnbanned,
    Cleanup
}