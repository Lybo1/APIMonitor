using AuthApi.Models;

namespace AuthApi.Services.AuthenticationServices.EmailServices.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="toEmail"></param>
    /// <param name="username"></param>
    /// <param name="sessionId"></param>
    /// <returns></returns>
    Task<EmailResponse> SendConfirmationEmailAsync(string toEmail, string username, string sessionId);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="toEmail"></param>
    /// <param name="username"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<EmailResponse> SendPasswordResetAsync(string toEmail, string username, string token);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="toEmail"></param>
    /// <param name="username"></param>
    /// <param name="htmlBody"></param>
    /// <param name="plainTextBody"></param>
    /// <returns></returns>
    Task<EmailResponse> SendSuspiciousActivityAsync(string toEmail, string username, string htmlBody, string plainTextBody);
    
}