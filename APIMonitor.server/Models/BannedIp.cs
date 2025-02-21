using System.ComponentModel.DataAnnotations;
using APIMonitor.server.Data;

namespace APIMonitor.server.Models;

public class BannedIp
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(Constants.Ipv4AddressLength, ErrorMessage = "Ipv4 cannot exceed 15 characters.")]
    public string IpAddress { get; set; } = null!;
    
    public DateTime BannedUntil { get; set; }
    
    [Required]
    public string Reason { get; set; } = "Repeated violations";
}