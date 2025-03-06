namespace APIMonitor.server.Models;

public class PacketInfo
{
    public string SourceIp { get; set; }
    public string DestinationIp { get; set; }
    public string SourceMac { get; set; }
    public string DestinationMac { get; set; }
    public string Protocol { get; set; }
    public int Length { get; set; }
    public DateTime Timestamp { get; set; }
    public string PayloadPreview { get; set; }
}