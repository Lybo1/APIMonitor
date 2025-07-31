using System.ComponentModel.DataAnnotations;
using APIMonitor.server.Data;

namespace APIMonitor.server.Models;

public class AlertRule
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(Constants.DescriptionLength, ErrorMessage = "Rule name cannot exceed 100 characters.")]
    public string RuleName { get; set; } = null!;
    
    [Required]
    [StringLength(Constants.DescriptionLength, ErrorMessage = "Description cannot exceed 100 characters.")]
    public string Description { get; set; } = null!;
    
    [Required]
    [StringLength(Constants.DescriptionLength, ErrorMessage = "Condition cannot exceed 100 characters.")]
    public string Condition { get; set; } = null!;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Threshold must be greater than 0.")]
    
    [DataType(DataType.Date)]
    public TimeSpan TimeWindow { get; set; }
    
    public bool IsActive { get; set; }
}