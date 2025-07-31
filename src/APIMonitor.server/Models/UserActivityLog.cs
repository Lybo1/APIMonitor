using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using APIMonitor.server.Data.Enumerations;
using APIMonitor.server.Identity;

namespace APIMonitor.server.Models;

public class UserActivityLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
        
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [Required]
    [EnumDataType(typeof(ActivityType))]
    public ActivityType Action { get; set; }

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    public string IpAddress { get; set; } = null!;

    [MaxLength(500)]
    public string? Details { get; set; }

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = null!;

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [Required]
    public bool IsSuccessful { get; set; } = true;
}