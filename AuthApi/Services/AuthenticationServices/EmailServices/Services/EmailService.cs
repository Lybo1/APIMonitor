using AuthApi.Models;
using MailKit.Net.Smtp;

namespace AuthApi.Services.AuthenticationServices.EmailServices.Services;

public class EmailService(SmtpClient smtpClient) : IEmailService
{
    
    private readonly byte[] bytes = new byte[64];


    public async Task<EmailResponse> SendConfirmationEmailAsync(string toEmail, string username)
    {
        throw new NotImplementedException();
    }

    public async Task<EmailResponse> SendPasswordResetAsync(string toEmail, string username, string token)
    {
        throw new NotImplementedException();
    }

    public async Task<EmailResponse> SendSuspiciousActivityAsync(string toEmail, string username, SuspiciousActivityDetails details)
    {
        throw new NotImplementedException();
    }
}