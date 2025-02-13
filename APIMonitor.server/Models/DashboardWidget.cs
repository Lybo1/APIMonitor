using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using APIMonitor.server.Data.Enumerations;
using APIMonitor.server.Identity;

namespace APIMonitor.server.Models;

public class DashboardWidget
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [Required]
    [EnumDataType(typeof(WidgetType))]
    public WidgetType WidgetType { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = "New Widget";

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public string Configuration { get; set; } = "{}";

    [Required]
    public int PositionX { get; set; } = 0;

    [Required]
    public int PositionY { get; set; } = 0;

    [Required]
    public int Width { get; set; } = 3;

    [Required]
    public int Height { get; set; } = 2;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}