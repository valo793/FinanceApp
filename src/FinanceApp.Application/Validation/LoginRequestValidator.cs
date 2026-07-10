using FinanceApp.Contracts.Auth;
using FluentValidation;

namespace FinanceApp.Application.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(160);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12);
    }
}
