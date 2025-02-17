using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using APIMonitor.server.Data;
using APIMonitor.server.Identity;

namespace APIMonitor.server.Models;

public class AuditLog
{
    public int Id { get; set; }
    
    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }

    [Required]
    [StringLength(Constants.Ipv4AddressLength, ErrorMessage = "Ipv4 cannot exceed 15 characters.")]
    public string Ipv4Address { get; set; } = null!;
    
    [Required]
    [StringLength(Constants.Ipv6AddressLength, ErrorMessage = "Ipv6 cannot exceed 39 characters.")]
    public string Ipv6Address { get; set; } = null!;
    
    [Required]
    [StringLength(Constants.MacAddressLength, ErrorMessage = "MAC address cannot exceed 50 characters.")]
    public string MacAddress { get; set; } = null!;
    
    [Required]
    [StringLength(500, ErrorMessage = "User-Agent cannot exceed 500 characters.")]
    public string UserAgent { get; set; } = "Unknown";
    
    [Required]
    [StringLength(Constants.DescriptionLength, ErrorMessage = "Action cannot exceed 100 characters.")]
    public string Action { get; set; } = null!;
    
    [Required]
    [StringLength(Constants.DetailsLength, ErrorMessage = "Details cannot exceed 1000 characters.")]
    public string Details { get; set; } = null!;
    
    [Required]
    public long ResponseTimeMs { get; set; } 
    
    [Required]
    [DataType(DataType.DateTime)]
    public DateTime Date { get; set; } = DateTime.UtcNow;
    
    [Required]
    [DataType(DataType.DateTime)]
    public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;
    
    public virtual User User { get; set; } = null!;
}