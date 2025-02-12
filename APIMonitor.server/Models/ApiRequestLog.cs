using System.ComponentModel.DataAnnotations;
using Microsoft.Identity.Web;
using Constants = APIMonitor.server.Data.Constants;

namespace APIMonitor.server.Models;

public class ApiRequestLog
{
    public int Id { get; set; }
    
    [DataType(DataType.Date)]
    public DateTime TimeStamp { get; set; } = DateTime.Now;

    [Required]
    [StringLength(Constants.Ipv4AddressLength, ErrorMessage = "IPv4 address cannot exceed 15 characters.")]
    public string IpAddress { get; set; } = null!;
    
    [StringLength(Constants.Ipv6AddressLength, ErrorMessage = "IPv6 address cannot exceed 39 characters.")]
    public string? Ipv6Address { get; set; } = null;
    
    [Required]
    [StringLength(Constants.HttpMethodLength, ErrorMessage = "HTTP method cannot exceed 10 characters.")]
    public string HttpMethod { get; set; } = null!;
    
    public int StatusCode { get; set; }
    
    [Required]
    public TimeSpan ResponseTime { get; set; }
    
    [Required]
    [StringLength(Constants.EndPointLength, ErrorMessage = "Endpoint cannot exceed 200 characters.")]
    public string Endpoint { get; set; } = null!;
    
    [DataType(DataType.MultilineText)]
    public string RequestPayload { get; set; } = null!;
    
    [DataType(DataType.MultilineText)]
    public string ResponsePayload { get; set; } = null!;
}