using System.Web;

namespace AuthApi.Services.AuthenticationServices.EmailServices.Services;

public class EmailContentProvider(IHostEnvironment environment, IConfiguration configuration)
{
    private readonly IHostEnvironment environment = environment;
    private readonly string baseUrl = configuration["App:BaseUrl"] ??
                                    throw new InvalidOperationException("App:BaseUrl is not configured");

    public (string HtmlBody, string PlainTextBody) GenerateConfirmationEmail(string firstName, string sessionId)
    {
        string confirmationUrl = $"{this.baseUrl}/api/auth/confirm?sessionId={Uri.EscapeDataString(sessionId)}";
        
        string htmlBody = $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Email Confirmation</title>
        </head>
        <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; line-height: 1.6; color: #333333; background-color: #f4f4f4;'>
            <table role='presentation' width='100%' style='width: 100%; border-collapse: collapse;'>
                <tr>
                    <td style='padding: 30px 0; text-align: center; background-color: #ffffff;'>
                        <table role='presentation' style='max-width: 600px; margin: 0 auto; padding: 0 20px;'>
                            <tr>
                                <td style='padding: 40px 0 30px;'>
                                    <h1 style='margin: 0; font-size: 24px; color: #2c3e50;'>Welcome, {HttpUtility.HtmlEncode(firstName)}!</h1>
                                </td>
                            </tr>
                            <tr>
                                <td style='padding-bottom: 20px;'>
                                    <p style='margin: 0; font-size: 16px;'>Thank you for creating an account. To complete your registration, please verify your email address:</p>
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 20px 0;'>
                                    <table role='presentation' style='margin: 0 auto;'>
                                        <tr>
                                            <td style='background-color: #007bff; border-radius: 4px; text-align: center;'>
                                                <a href='{HttpUtility.HtmlEncode(confirmationUrl)}'
                                                   style='display: inline-block; padding: 12px 24px; color: #ffffff; text-decoration: none; font-weight: bold;'>
                                                    Verify Email Address
                                                </a>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 20px 0;'>
                                    <p style='margin: 0; font-size: 14px; color: #666666;'>
                                        This verification link will expire in 24 hours for security reasons.
                                        If you didn't create an account, please ignore this email.
                                    </p>
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 20px 0; border-top: 1px solid #eeeeee;'>
                                    <p style='margin: 0; font-size: 14px; color: #666666;'>
                                        Best regards,<br>
                                        The {HttpUtility.HtmlEncode(configuration["App:Name"] ?? "App")} Team
                                    </p>
                                </td>
                            </tr>
                            <tr>
                                <td style='padding-top: 20px;'>
                                    <p style='margin: 0; font-size: 12px; color: #999999;'>
                                        If the button doesn't work, copy and paste this link into your browser:<br>
                                        <span style='color: #007bff;'>{HttpUtility.HtmlEncode(confirmationUrl)}</span>
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>";

        string plainTextBody = $"""
        Thank you for creating an account with {configuration["App:Name"] ?? "App"}
        
        Hello {firstName},
        
        Thank you for creating an account. To complete your registration, please verify your email address by clicking the link below or copying it into your browser:
        
        {confirmationUrl}
        
        This verification link will expire in 24 hours for security reasons.
        If you didn't create an account, please ignore this email.
        
        Best regards,
        The {configuration["App:Name"] ?? "App"} Team
        
        ---
        This is an automated message, please do not reply to this email.
        """;
        
        return (htmlBody, plainTextBody);
    }
}