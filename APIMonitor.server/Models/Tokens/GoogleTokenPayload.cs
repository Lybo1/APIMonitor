using System.Text.Json.Serialization;

namespace APIMonitor.server.Models;

public class GoogleTokenPayload
{
    [JsonPropertyName("aud")]
    public string Aud { get; set; }
        
    [JsonPropertyName("email")]
    public string Email { get; set; }
        
    [JsonPropertyName("exp")]
    public long Exp { get; set; }
}