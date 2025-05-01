using AuthApi.Models;

namespace AuthApi.Services.AuthenticationServices.EmailServices;

public interface IEmailService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="toEmail"></param>
    /// <param name="username"></param>
    /// <returns></returns>
    Task<EmailResponse> SendConfirmationEmailAsync(string toEmail, string username);
    
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
    /// <param name="details"></param>
    /// <returns></returns>
    Task<EmailResponse> SendSuspiciousActivityAsync(string toEmail, string username, SuspiciousActivityDetails details);
    
}