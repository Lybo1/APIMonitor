using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace APIMonitor.server.Identity;

public class User : IdentityUser
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
    
    [MaxLength(500)]
    public string? RefreshToken { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [DataType(DataType.DateTime)]
    [Required(ErrorMessage = "Refresh token expiry date is required.")]
    public DateTime RefreshTokenExpiry { get; set; }

    [Range(0, 3, ErrorMessage = "Failed login attempts cannot be negative.")]
    public int FailedLoginAttempts { get; set; } = 0;

    public bool RememberMe { get; set; }
    public bool IsLockedOut { get; set; } = false;
    public bool IsAdmin { get; set; } = false;
}