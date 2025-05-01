namespace AuthApi.Services.AuthenticationServices.EmailServices.Configuration;

public class EmailOptions
{
    public string SmtpHost { get; set; } = null!;
    public int SmtpPort { get; set; }
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
}