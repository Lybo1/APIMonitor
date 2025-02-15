using System.ComponentModel.DataAnnotations;
using APIMonitor.server.Data;

namespace APIMonitor.server.Models;

public class TrustedDevice
{
    public int Id { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    
    [StringLength(Constants.Ipv4AddressLength, ErrorMessage = "Ip address length should not exceed 15 characters.")]
    public string IpAddress { get; set; } = string.Empty;
    
    public string UserAgent { get; set; } = string.Empty;
    
    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}