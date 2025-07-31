using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using APIMonitor.server.Identity;

namespace APIMonitor.server.Models;

public class NotificationPreference
{
    public int Id { get; set; }
    
    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }
    
    public bool EnablePush { get; set; } = true;
    public bool EnableCriticalAlertsOnly { get; set; } = false;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual User User { get; set; } = null!;
}