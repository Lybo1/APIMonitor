using System.ComponentModel.DataAnnotations;
using APIMonitor.server.Data;

namespace APIMonitor.server.Models;

public class IpGeolocation
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(Constants.Ipv4AddressLength, ErrorMessage = "Ipv4 cannot exceed 15 characters.")]
    public string Ipv4Address { get; set; } = null!;
    
    [Required]
    [StringLength(Constants.Ipv6AddressLength, ErrorMessage = "Ipv6 cannot exceed 39 characters.")]
    public string Ipv6Address { get; set; } = null!;
    
    [Required]
    [StringLength(Constants.CountryLength, ErrorMessage = "Country cannot exceed 100 characters.")]
    public string Country { get; set; } = null!;
    
    [Required]
    [StringLength(Constants.CountryLength, ErrorMessage = "Country cannot exceed 100 characters.")]
    public string City { get; set; } = null!;
    
    [Required]
    [StringLength(Constants.LatitudeLength, ErrorMessage = "Longitude cannot exceed 50 characters.")]
    public string Latitude { get; set; } = null!;
    
    [Required]
    [StringLength(Constants.LongitudeLength, ErrorMessage = "Longitude cannot exceed 50 characters.")]
    public string Longitude { get; set; } = null!;
}