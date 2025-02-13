using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using APIMonitor.server.Data;
using APIMonitor.server.Models;
using Microsoft.AspNetCore.Identity;

namespace APIMonitor.server.Identity;

public class User : IdentityUser<int>
{
    
    
    [Required]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ\s]+$", ErrorMessage = "First name must contain only letters and spaces.")]
    public string FirstName { get; set; } = null!;

    [Required]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ\s]+$", ErrorMessage = "Last name must contain only letters and spaces.")]
    public string LastName { get; set; } = null!;
    
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
    
    [MaxLength(Constants.RefreshTokenLength)]
    public string? RefreshToken { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required(ErrorMessage = "Refresh token expiry date is required.")]
    [DataType(DataType.DateTime)]
    public DateTime RefreshTokenExpiry { get; set; }

    [Range(0, 3, ErrorMessage = "Failed login attempts cannot be negative.")]
    public int FailedLoginAttempts { get; set; } = 0;

    public bool RememberMe { get; set; }
    public bool IsLockedOut { get; set; } = false;
    public bool IsAdmin { get; set; } = false;
    
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<EventLog> EventLogs { get; set; } = new List<EventLog>();
    public virtual ICollection<UserActivityLog> ActivityLogs { get; set; } = new List<UserActivityLog>();
    public virtual ICollection<DashboardWidget> DashboardWidgets { get; set; } = new List<DashboardWidget>();
}