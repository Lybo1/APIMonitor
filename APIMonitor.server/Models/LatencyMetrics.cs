using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIMonitor.server.Models;

public class LatencyMetrics
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required] 
    [MaxLength(50)]
    public string DnsResolution { get; set; }
    
    [Required] 
    [MaxLength(50)]
    public string Connect { get; set; }
    
    [Required] 
    [MaxLength(50)]
    public string TotalRequest { get; set; }
}