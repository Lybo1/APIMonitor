using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIMonitor.server.Models;

public class TokenResponse
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string AccessToken { get; set; } = null!;
    
    [Required]
    [MaxLength(500)]
    public string RefreshToken { get; set; } = null!;
}