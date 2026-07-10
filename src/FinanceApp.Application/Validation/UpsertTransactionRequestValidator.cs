using FinanceApp.Contracts.Transactions;
using FluentValidation;

namespace FinanceApp.Application.Validation;

public sealed class UpsertTransactionRequestValidator : AbstractValidator<UpsertTransactionRequest>
{
    public UpsertTransactionRequestValidator()
    {
        RuleFor(x => x.TransactionType).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(300);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x)
            .Must(x => x.AmountActual.HasValue || x.AmountExpected.HasValue)
            .WithMessage("A transação deve conter ao menos um valor.");
    }
}
