using System.ComponentModel.DataAnnotations;

namespace APIMonitor.server.Models;

public class RateLimitViolation
{
    public int Id { get; set; }
    
    public string UserId { get; set; } = null!;

    [Required]
    [StringLength(15)]
    public string IpAddress { get; set; } = null!;
    
    [Required]
    [StringLength(150)]
    public string Action { get; set; } = null!;
    
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public TimeSpan PenaltyDuration { get; set; }
}