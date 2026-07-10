using FinanceApp.Contracts.Accounts;
using FluentValidation;

namespace FinanceApp.Application.Validation;

public sealed class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.AccountType).NotEmpty().MaximumLength(30);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.OpeningBalance).GreaterThanOrEqualTo(0);
    }
}
