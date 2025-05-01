using System.Text.RegularExpressions;
using AuthApi.Services.AuthenticationServices.EmailServices.Configuration;
using FluentValidation;

namespace AuthApi.Validators;

/// <summary>
/// Validates the <see cref="EmailOptions"/> configuration to ensure secure and functional SMTP settings.
/// This validator enforces strict rules for hostname, port, username, and password, with a focus on 
/// security through regex patterns and entropy checks, preparing for future encryption with Argon2 and AES.
/// </summary>
public class EmailOptionsValidator : AbstractValidator<EmailOptions>
{
    /// <summary>
    /// Regular expression for validating SMTP hostnames according to RFC 1035.
    /// Ensures a hostname is 1–63 characters per label, with a total length up to 253 characters,
    /// supporting modern TLDs and preventing injection attempts.
    /// </summary>
    private static readonly Regex ValidHostname = new(@"^(?!-)[a-zA-Z0-9-]{1,63}(?:\.(?!-)[a-zA-Z0-9-]{1,63})*\.[a-zA-Z]{2,63}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    /// <summary>
    /// Regular expression for validating passwords.
    /// Requires at least one uppercase letter, one lowercase letter, one digit, and one special character
    /// from a defined set, with a length of 12–64 characters to balance security and usability.
    /// </summary>
    private static readonly Regex ValidPassword = new(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[!@#$%^&*()\-_=+{};:,<.>])[A-Za-z\d!@#$%^&*()\-_=+{};:,<.>]{12,64}$", RegexOptions.Compiled);
    
    /// <summary>
    /// Minimum entropy in bits required for passwords (60 bits).
    /// Ensures a baseline of randomness, rejecting weak or predictable patterns.
    /// </summary>
    private const double MinimumBits = 60.0;
    
    /// <summary>
    /// Maximum entropy in bits allowed for passwords (256 bits).
    /// Caps entropy at a quantum-resistant level (128 bits post-Grover’s algorithm),
    /// aligning with AES-256 key sizes and practical limits.
    /// </summary>
    private const double MaximumBits = 256.0;
    
    /// <summary>
    /// Minimum number of unique characters required in passwords (8).
    /// Prevents excessive repetition, enhancing diversity and strength.
    /// </summary>
    private const int MinimumUniqueChars = 8;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailOptionsValidator"/> class.
    /// Defines validation rules for SMTP configuration properties with a focus on security and correctness.
    /// </summary>
    public EmailOptionsValidator()
    {
        RuleFor(x => x.SmtpHost)
            .NotEmpty().WithMessage("SMTP host is required.")
            .Must(host => host.Length <= 253 && ValidHostname.IsMatch(host)).WithMessage("SMTP host must be a valid hostname or IP address.");
        
        RuleFor(x => x.SmtpPort)
            .InclusiveBetween(1, 65535).WithMessage("SMTP port must be between 1 and 65535.");
        
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
            .MaximumLength(50).WithMessage("Username cannot exceed 100 characters.");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("SMTP password is required.")
            .MinimumLength(12).WithMessage("SMTP password must be at least 12 characters.")
            .Must(password => ValidPassword.IsMatch(password))
            .WithMessage("SMTP password must include at least one letter, one number and one special character.")
            .Must(BeStrongPassword)
            .WithMessage($"SMTP password must have at least {MinimumBits} bits of entropy.");
    }

    /// <summary>
    /// Determines if a password meets strength requirements based on regex, entropy, and unique character count.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <returns><c>true</c> if the password is strong; otherwise, <c>false</c>.</returns>
    private static bool BeStrongPassword(string password)
    {
        if (!ValidPassword.IsMatch(password))
        {
            return false;
        }
        
        double entropy = CalculatePasswordEntropy(password);
        int uniqueChars = password.Distinct().Count();
        
        return entropy is >= MinimumBits and <= MaximumBits && uniqueChars >= MinimumUniqueChars;
    }
    
    /// <summary>
    /// Calculates the Shannon entropy of a password in bits, based on character frequency.
    /// Higher entropy indicates greater randomness and resistance to guessing attacks.
    /// </summary>
    /// <param name="password">The password to analyze.</param>
    /// <returns>The entropy in bits, or 0 if the password is null or empty.</returns>
    private static double CalculatePasswordEntropy(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return 0;
        }

        Dictionary<char, int> charCounts = new();

        foreach (char c in password)
        {
            charCounts[c] = charCounts.GetValueOrDefault(c) + 1;
        }
        
        double entropy = 0;
        int length = password.Length;

        foreach (int count in charCounts.Values)
        {
            double probability = (double)count / length;
            entropy -= probability * Math.Log2(probability);
        }

        return entropy * length;
    }
}