namespace APIMonitor.server.Models;

public class ApiScanResult
{
    public string Status { get; set; } 
    public LatencyMetrics Latency { get; set; }
    public List<string> Headers { get; set; }
    public string BodySnippet { get; set; }
    public string Health { get; set; }
    public string ColorHint { get; set; }
    public List<PacketInfo> Packets { get; set; }
}