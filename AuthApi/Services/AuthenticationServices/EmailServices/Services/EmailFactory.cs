using MimeKit;

namespace AuthApi.Services.AuthenticationServices.EmailServices.Services;

public class EmailFactory(IConfiguration config)
{
    private readonly IConfiguration config = config ?? throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
    private readonly string senderEmail = config["Email:SenderEmail"] ?? throw new InvalidOperationException(nameof(senderEmail)); 
    private readonly string senderName = config["Email:SenderName"] ?? throw new InvalidOperationException(nameof(senderName));

    public MimeMessage CreateConfirmationMail(string toMail, string firstName, string sessionId, EmailContentProvide contentProvider)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(toMail, nameof(toMail));

        var (htmlBody, plainTextBody) = contentProvider.GenerateConfirmationEmail(firstName, sessionId);

        return CreateConfirmationMail(toMail, "Confirmation Email", htmlBody, plainTextBody);
    }

    public MimeMessage CreateMail(string toEmail, string subject, string htmlBody, string plainTextBody)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(toEmail, nameof(toEmail));
        ArgumentNullException.ThrowIfNullOrEmpty(subject, nameof(subject));
        ArgumentNullException.ThrowIfNullOrEmpty(htmlBody, nameof(htmlBody));
        ArgumentNullException.ThrowIfNullOrEmpty(plainTextBody, nameof(plainTextBody));

        string sanitizedSubject = subject.Replace("\r", "").Replace("\n", "");
    }
}