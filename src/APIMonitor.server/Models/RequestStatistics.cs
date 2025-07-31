using System.ComponentModel.DataAnnotations;

namespace APIMonitor.server.Models;

public class RequestStatistics
{
    public int Id { get; set; }
    
    public TimeSpan TimeSlot { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Requests per minute must be greater than 0.")]
    public int AverageRequestsCount { get; set; }
    
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Error count must be a positive number.")]
    public int ErrorsCount { get; set; }
}