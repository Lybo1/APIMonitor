using System.ComponentModel.DataAnnotations;
using APIMonitor.server.Data;

namespace APIMonitor.server.Models;

public class IpBlock
{
    public int Id { get; set; }
    
    public int FailedAttempts { get; set; }
    
    [Required]
    [StringLength(Constants.Ipv4AddressLength, ErrorMessage = "IP address cannot exceed 100 characters.")]
    public string Ip { get; set; } = null!;
    
    [Required]
    [DataType(DataType.Date)]
    public DateTime BlockedUntil { get; set; } = DateTime.UtcNow;
    
    [Required]
    [StringLength(Constants.DescriptionLength, ErrorMessage = "Reason cannot exceed 100 characters.")]
    public string Reason { get; set; } = null!;
}