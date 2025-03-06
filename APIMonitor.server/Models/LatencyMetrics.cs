namespace APIMonitor.server.Models;

public class LatencyMetrics
{
    public string DnsResolution { get; set; }
    public string Connect { get; set; }
    public string TotalRequest { get; set; }
}