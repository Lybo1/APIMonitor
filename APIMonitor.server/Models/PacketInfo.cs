using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIMonitor.server.Models;

public class PacketInfo
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required] 
    [MaxLength(45)]
    public string SourceIp { get; set; } = null!;
    
    [Required] 
    [MaxLength(45)]
    public string DestinationIp { get; set; } = null!;
    
    [Required] 
    [MaxLength(17)]
    public string SourceMac { get; set; } = null!;
    
    [Required] 
    [MaxLength(17)]
    public string DestinationMac { get; set; } = null!;
    
    [Required] 
    [MaxLength(20)]
    public string Protocol { get; set; } = null!;
    
    [Required]
    [Range(0, int.MaxValue)]
    public int Length { get; set; }
    
    [Required]
    public DateTime Timestamp { get; set; }
    
    [MaxLength(200)]
    public string PayloadPreview { get; set; } = null!;
}