using System.ComponentModel.DataAnnotations;
using APIMonitor.server.Data;

namespace APIMonitor.server.Models;

public class BotDetectionLog
{
    public int Id { get; set; }

    [Required]
    [StringLength(Constants.Ipv4AddressLength, ErrorMessage = "Ip cannot exceed 15 characters.")]
    public string IpAddress { get; set; } = null!;
    
    [Required]
    [StringLength(Constants.UserAgentLength, ErrorMessage = "Ip cannot exceed 15 characters.")]
    public string UserAgent { get; set; } = null!;
    
    [Required]
    [StringLength(Constants.BotSignatureLength, ErrorMessage = "Bot signature cannot exceed 200 characters.")]
    public string BotSignature { get; set; } = null!;

    [Required]
    [StringLength(Constants.DescriptionLength, ErrorMessage = "Description cannot exceed 100 characters.")]
    public string Description { get; set; } = null!;
    
    [Required]
    [DataType(DataType.DateTime)]
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}