using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using APIMonitor.server.Data;
using APIMonitor.server.Data.Enumerations;
using APIMonitor.server.Identity;

namespace APIMonitor.server.Models;

public class Notification
{
    public Guid Id { get; set; }
    
    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }
    
    [Required]
    [StringLength(Constants.TitleLength, ErrorMessage = "Title cannot exceed 200 characters.")]
    public string Title { get; set; } = null!;
    
    [Required]
    [StringLength(Constants.DetailsLength, ErrorMessage = "Message cannot exceed 1000 characters.")]
    public string Message { get; set; } = null!;
    
    [Required]
    [EnumDataType(typeof(NotificationType))]
    public NotificationType Type { get; set; }
    
    public bool IsRead { get; set; }
    [Required]
    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual User User { get; set; } = null!;
}