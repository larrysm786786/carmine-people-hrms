using FluentValidation;

namespace CarminePeople.Application.Common.Validation;

// Shared password-strength rule, applied everywhere a password is set (employee creation,
// password reset) so the requirement can't silently diverge between the two call sites.
public static class PasswordRuleExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeAStrongPassword<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
}
