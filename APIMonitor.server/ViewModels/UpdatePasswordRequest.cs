namespace APIMonitor.server.ViewModels;

public class UpdatePasswordRequest
{
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
}