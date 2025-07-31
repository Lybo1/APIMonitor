using System.ComponentModel.DataAnnotations;
using APIMonitor.server.Data;

namespace APIMonitor.server.Models;

public class BlockIpRequest
{
    [Required]
    [StringLength(Constants.Ipv4AddressLength, ErrorMessage = "Ipv4 length cannot exceed 15 characters.")]
    public string IpAddress { get; set; } = null!;
    
    public int DurationHours { get; set; }
    
    [Required]
    public string Reason { get; set; } = null!;
}