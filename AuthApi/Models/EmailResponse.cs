namespace AuthApi.Models;

public class EmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public string Code { get; set; } = null!;
}