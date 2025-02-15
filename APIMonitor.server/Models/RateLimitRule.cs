using System.ComponentModel.DataAnnotations;
using APIMonitor.server.Data;

namespace APIMonitor.server.Models;

public class RateLimitRule
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(150)]
    public string Action { get; set; } = string.Empty;
    
    [Required]
    [StringLength(Constants.Ipv4AddressLength, ErrorMessage = "Ip address length cannot exceed 15 characters.")]
    public string IpAddress { get; set; } = null!;
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Rate limit must be greater than zero.")]
    public int MaxRequests { get; set; }
    
    [Required]
    public TimeSpan TimeWindow { get; set; }
    
    public bool IsActive { get; set; }
}