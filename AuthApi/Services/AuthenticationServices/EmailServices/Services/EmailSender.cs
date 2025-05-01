using System.Net;
using System.Net.Mail;
using AuthApi.Services.AuthenticationServices.EmailServices.Configuration;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace AuthApi.Services.AuthenticationServices.EmailServices.Services;

/// <summary>
/// 
/// </summary>
/// <param name="smtpClient"></param>
/// <param name="logger"></param>
/// <param name="emailOptions"></param>
/// <param name="disposed"></param>
public class EmailSender(ILogger<EmailService> logger, IOptions<EmailOptions> options) : IEmailSender, IDisposable
{
    private readonly EmailOptions emailOptions = options?.Value ?? throw new ArgumentNullException(nameof(emailOptions));
    private readonly ILogger<EmailService> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private SmtpClient smtpClient;
    private bool disposed;
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="email"></param>
    /// <param name="subject"></param>
    /// <param name="htmlMessage"></param>
    /// <exception cref="NotImplementedException"></exception>
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="email"></param>
    /// <param name="subject"></param>
    /// <param name="htmlMessage"></param>
    public async Task SendResetPasswordEmailAsync(string email, string subject, string htmlMessage)
    {
        
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="email"></param>
    /// <param name="subject"></param>
    /// <param name="htmlMessage"></param>
    public async Task SendNotificationEmailAsync(string email, string subject, string htmlMessage)
    {
        
    }
    
    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        smtpClient.Dispose();
    }

    /// <summary>
    /// 
    /// </summary>
    private void ConfigureSmtpClient()
    {
        this.smtpClient = new SmtpClient(this.emailOptions.SmtpHost, this.emailOptions.SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(this.emailOptions.UserName, this.emailOptions.Password)
            {
                
            },
            Timeout = 5000,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };
    }
}