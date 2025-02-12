namespace APIMonitor.server.Models;

public class TokenResponse
{
    public int Id { get; set; }
    
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
}