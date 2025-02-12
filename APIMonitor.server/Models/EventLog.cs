using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using APIMonitor.server.Data;
using APIMonitor.server.Data.Enumerations;
using APIMonitor.server.Identity;

namespace APIMonitor.server.Models;

public class EventLog
{
    public int Id { get; set; }
    
    [DataType(DataType.Date)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public EventType EventType { get; set; }

    [Required]
    [StringLength(Constants.DescriptionLength, ErrorMessage = "Description cannot be longer than 100 characters.")]
    public string Description { get; set; } = null!;
    
    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }
    
    public virtual User User { get; set; } = null!;
}