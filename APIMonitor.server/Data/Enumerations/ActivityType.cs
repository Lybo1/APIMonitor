namespace APIMonitor.server.Data.Enumerations;

public enum ActivityType
{
    Login,
    Logout,
    PasswordChange,
    ProfileUpdate,
    SecuritySettingChange,
    CreatedAlert,
    DeletedAlert,
    ApiKeyRegenerated,
    FailedLoginAttempt,
    DashboardModified,
    Other
}