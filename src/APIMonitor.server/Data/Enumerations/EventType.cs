namespace APIMonitor.server.Data.Enumerations;

public enum EventType
{
        UserLoginAttempt,
        UserLoginSuccess,
        UserLoginFailure,
        UserLogout,
        UserAccountLocked,
        UserAccountUnlocked,
        UserRoleAssigned,
        UserRoleRemoved,
        UserPasswordChange,
        UserProfileUpdated,
        
        ApiRequestReceived,
        ApiRequestProcessed,
        ApiRequestFailed,
        ApiRequestTimeout,
        ApiRequestRateLimitExceeded,
        ApiRequestBlocked,
        
        SystemStart,
        SystemShutdown,
        SystemError,
        SystemUpdate,
        ServiceUnavailable,
        
        SuspiciousActivityDetected,
        UnauthorizedAccessAttempt,
        IpBlocked,
        BruteForceAttackDetected,
        
        AdminUserCreated,
        AdminUserUpdated,
        AdminUserDeleted,
        RoleCreated,
        RoleUpdated,
        RoleDeleted,
        
        AlertTriggered,
        AlertResolved,
        AlertAcknowledged,
        
        DataImported,
        DataExported,
        DataDeleted,


}