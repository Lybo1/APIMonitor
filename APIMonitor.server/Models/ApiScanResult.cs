using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIMonitor.server.Models;

public class ApiScanResult
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required] 
    [MaxLength(50)]
    public string Status { get; set; } 
    
    [Required]
    public LatencyMetrics Latency { get; set; }
    
    [Required]
    public List<string> Headers { get; set; }
    
    [MaxLength(500)]
    public string BodySnippet { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Health { get; set; }
    
    [MaxLength(20)]
    public string ColorHint { get; set; }
    
    [Required]
    public List<PacketInfo> Packets { get; set; }
}