using AuthApi.Identity;
using FluentValidation;

namespace AuthApi.Validators;

public class UserValidator : AbstractValidator<User>
{
    private const string EmailRegex = @"^[a-zA-Z0-9._%+-]{1,64}@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
    
    public UserValidator()
    {
        RuleFor(u => u.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(256).WithMessage("Email must be at most 256 characters");

            RuleFor(u => u.NormalizedEmail)
                .NotEmpty().WithMessage("Normalized email is required")
                .MaximumLength(256).WithMessage("Normalized email must be at most 256 characters")
                .Must(email => email == email?.ToUpperInvariant())
                .WithMessage("Normalized email must be uppercase");

            RuleFor(u => u.PasswordHash)
                .NotEmpty().WithMessage("Password hash is required")
                .When(u => u.Id == 0 || u.PasswordHash != null, ApplyConditionTo.CurrentValidator);

            RuleFor(u => u.SecurityStamp)
                .NotEmpty().WithMessage("Security stamp is required");

            RuleFor(u => u.EmailConfirmed)
                .Equal(false).WithMessage("Email must not be confirmed on creation")
                .When(u => u.Id == 0, ApplyConditionTo.CurrentValidator);

            RuleFor(u => u.FirstName)
                .NotNull().WithMessage("First name cannot be null")
                .MaximumLength(50).WithMessage("First name must be at most 50 characters")
                .Matches(@"^[a-zA-Z\s-]*$").WithMessage("First name can only contain letters, spaces, or hyphens")
                .When(u => u.FirstName != string.Empty, ApplyConditionTo.CurrentValidator);

            RuleFor(u => u.LastName)
                .NotNull().WithMessage("Last name cannot be null")
                .MaximumLength(50).WithMessage("Last name must be at most 50 characters")
                .Matches(@"^[a-zA-Z\s-]*$").WithMessage("Last name can only contain letters, spaces, or hyphens")
                .When(u => u.LastName != string.Empty, ApplyConditionTo.CurrentValidator);

            RuleFor(u => u.IsAdmin)
                .Equal(false).WithMessage("Users cannot be created as admins")
                .When(u => u.Id == 0, ApplyConditionTo.CurrentValidator);

            RuleFor(u => u.RememberMe)
                .Equal(false).WithMessage("RememberMe must be false on creation")
                .When(u => u.Id == 0, ApplyConditionTo.CurrentValidator);

            RuleFor(u => u.CreatedAt)
                .NotEmpty().WithMessage("CreatedAt is required")
                .Must(date => date <= DateTime.UtcNow).WithMessage("CreatedAt cannot be in the future")
                .Must(date => date >= DateTime.UtcNow.AddDays(-1)).WithMessage("CreatedAt cannot be more than one day in the past");

            RuleFor(u => u.LastLogin)
                .NotEmpty().WithMessage("LastLogin is required")
                .Must(date => date <= DateTime.UtcNow).WithMessage("LastLogin cannot be in the future")
                .Must(date => date >= DateTime.UtcNow.AddDays(-1)).WithMessage("LastLogin cannot be more than one day in the past")
                .Equal(u => u.CreatedAt).WithMessage("LastLogin must equal CreatedAt on creation")
                .When(u => u.Id == 0, ApplyConditionTo.CurrentValidator);
    }
}