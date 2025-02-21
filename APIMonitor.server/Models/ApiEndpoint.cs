using System.ComponentModel.DataAnnotations;

namespace APIMonitor.server.Models;

public class ApiEndpoint
{
    public int Id { get; set; }

    [Required]
    [Url]
    public string Url { get; set; } = null!;

    public bool IsActive { get; set; } = true;
}