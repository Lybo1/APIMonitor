using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using APIMonitor.server.Data;
using APIMonitor.server.Identity;

namespace APIMonitor.server.Models;

public class TrustedDevice
{
    public int Id { get; set; }
    
    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }
    
    [StringLength(Constants.Ipv4AddressLength, ErrorMessage = "Ip address length should not exceed 15 characters.")]
    public string IpAddress { get; set; } = string.Empty;
    
    [StringLength(75, ErrorMessage = "User agent should not exceed 75 characters.")]
    public string UserAgent { get; set; } = string.Empty;
    
    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual User User { get; set; } = null!;
}