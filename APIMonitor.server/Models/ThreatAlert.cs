using System.ComponentModel.DataAnnotations;
using APIMonitor.server.Data;
using APIMonitor.server.Data.Enumerations;

namespace APIMonitor.server.Models;

public class ThreatAlert
{
    public int Id { get; set; }
    
    [Required]
    [DataType(DataType.Date)]
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

    [Required]
    [EnumDataType(typeof(AlertType))]
    public AlertType AlertType { get; set; }

    [Required]
    [StringLength(Constants.DescriptionLength, ErrorMessage = "Description length cannot exceed 100 characters.")]
    public string Description { get; set; } = null!;
    
    [Required]
    [StringLength(Constants.Ipv4AddressLength, ErrorMessage = "IPv4 address length cannot exceed 15 characters.")]
    public string IpAddress { get; set; } = null!;
    
    [EnumDataType(typeof(AlertSeverity))]
    public AlertSeverity Severity { get; set; }
    
    public bool IsResolved { get; set; }
    
    [DataType(DataType.Date)]
    public DateTime? ResolvedAt { get; set; } = DateTime.UtcNow;
}