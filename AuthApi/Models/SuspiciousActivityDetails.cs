namespace AuthApi.Models;

public class SuspiciousActivityDetails
{
    public DateTime ActivityDate { get; set; }
    public string IpAddress { get; set; } = null!;
    public string DeviceInfo { get; set; } = null!;
    public string ActionUrl { get; set; } = null!;
}