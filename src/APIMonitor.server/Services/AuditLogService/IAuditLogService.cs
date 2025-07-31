using APIMonitor.server.Models;

namespace APIMonitor.server.Services.AuditLogService;

public interface IAuditLogService
{
    Task LogActionAsync(int userId, string action, string details, DateTime requestStartTime);
    Task<List<AuditLog>> GetUserAuditLogsAsync(int userId);
    Task<AuditLog?> GetAuditLogByIdAsync(int id, int? userId = null);
    Task<List<AuditLog>> GetAllAuditLogsAsync();
    Task<bool> DeleteAuditLogAsync(int id);
    Task<bool> PurgeAuditLogsAsync();

}