using System.Net;
using System.Net.Mail;
using AuthApi.Services.AuthenticationServices.EmailServices.Configuration;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace AuthApi.Services.AuthenticationServices.EmailServices.Services;

/// <summary>
/// 
/// </summary>
/// <param name="logger"></param>
/// <param name="options"></param>
public class EmailSender(ILogger<EmailService> logger, IOptions<EmailOptions> options) : IEmailSender, IDisposable
{
    
}