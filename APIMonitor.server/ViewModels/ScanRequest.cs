using System.ComponentModel.DataAnnotations;

namespace APIMonitor.server.ViewModels;

public class ScanRequest
{
    [Required]
    public string ApiUrl { get; set; }

    public string Method { get; set; } = "GET";

    public string? ApiKey { get; set; }

    public bool ForceRefresh { get; set; } = false;
}