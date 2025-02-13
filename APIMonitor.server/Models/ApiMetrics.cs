using System.ComponentModel.DataAnnotations;

namespace APIMonitor.server.Models;

public class ApiMetrics
{
    public int Id { get; set; }
    
    [Required]
    [DataType(DataType.DateTime )]
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Total requests must be greater than 0.")]
    public int TotalRequests { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Requests per minute must be greater than 0.")]
    public int RequestsPerMinute { get; set; }
    
    public TimeSpan AverageResponseTime { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Error count must be a positive number.")]
    public int ErrorsCount { get; set; }
}