using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Services;
using FluentAssertions;
using Xunit;

namespace FinanceApp.UnitTests;

public sealed class BalanceCalculatorTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Should_calculate_confirmed_delta_for_account()
    {
        var accountId = Guid.NewGuid();

        var transactions = new List<Transaction>
        {
            new(UserId, TransactionTypes.Income, "Salário", TransactionStatuses.Confirmed, new DateOnly(2026, 3, 1), 1000m, 1000m, "BRL", accountId),
            new(UserId, TransactionTypes.Expense, "Aluguel", TransactionStatuses.Confirmed, new DateOnly(2026, 3, 5), 400m, 400m, "BRL", accountId),
            new(UserId, TransactionTypes.Transfer, "Transferência", TransactionStatuses.Confirmed, new DateOnly(2026, 3, 6), 200m, 200m, "BRL", accountId, Guid.NewGuid())
        };

        var result = BalanceCalculator.CalculateConfirmedDelta(transactions, accountId);

        result.Should().Be(400m);
    }

    [Fact]
    public void No_transactions_returns_zero()
    {
        var accountId = Guid.NewGuid();
        var result = BalanceCalculator.CalculateConfirmedDelta([], accountId);
        result.Should().Be(0m);
    }

    [Fact]
    public void Planned_transactions_are_ignored()
    {
        var accountId = Guid.NewGuid();

        var transactions = new List<Transaction>
        {
            new(UserId, TransactionTypes.Income, "Salário futuro", TransactionStatuses.Planned, new DateOnly(2026, 4, 1), 5000m, null, "BRL", accountId),
            new(UserId, TransactionTypes.Expense, "Conta futura", TransactionStatuses.Planned, new DateOnly(2026, 4, 5), 1500m, null, "BRL", accountId),
            new(UserId, TransactionTypes.Income, "Freelance", TransactionStatuses.Confirmed, new DateOnly(2026, 3, 15), 800m, 800m, "BRL", accountId)
        };

        var result = BalanceCalculator.CalculateConfirmedDelta(transactions, accountId);

        result.Should().Be(800m, "only confirmed income should count");
    }

    [Fact]
    public void Deleted_transactions_are_ignored()
    {
        var accountId = Guid.NewGuid();

        var income = new Transaction(UserId, TransactionTypes.Income, "Salário", TransactionStatuses.Confirmed, new DateOnly(2026, 3, 1), 2000m, 2000m, "BRL", accountId);
        var deletedExpense = new Transaction(UserId, TransactionTypes.Expense, "Deletado", TransactionStatuses.Confirmed, new DateOnly(2026, 3, 2), 500m, 500m, "BRL", accountId);
        deletedExpense.MarkDeleted(UserId);

        var transactions = new List<Transaction> { income, deletedExpense };

        var result = BalanceCalculator.CalculateConfirmedDelta(transactions, accountId);

        result.Should().Be(2000m, "deleted transactions must be excluded from calculation");
    }

    [Fact]
    public void Transactions_from_other_accounts_are_isolated()
    {
        var myAccount = Guid.NewGuid();
        var otherAccount = Guid.NewGuid();

        var transactions = new List<Transaction>
        {
            new(UserId, TransactionTypes.Income, "Meu salário", TransactionStatuses.Confirmed, new DateOnly(2026, 3, 1), 3000m, 3000m, "BRL", myAccount),
            new(UserId, TransactionTypes.Income, "Receita alheia", TransactionStatuses.Confirmed, new DateOnly(2026, 3, 1), 9999m, 9999m, "BRL", otherAccount),
            new(UserId, TransactionTypes.Expense, "Minha conta", TransactionStatuses.Confirmed, new DateOnly(2026, 3, 5), 1000m, 1000m, "BRL", myAccount)
        };

        var result = BalanceCalculator.CalculateConfirmedDelta(transactions, myAccount);

        result.Should().Be(2000m, "only transactions belonging to myAccount should be considered");
    }

    [Fact]
    public void Transfer_credits_destination_account()
    {
        var source = Guid.NewGuid();
        var dest = Guid.NewGuid();

        var transactions = new List<Transaction>
        {
            new(UserId, TransactionTypes.Transfer, "Pix", TransactionStatuses.Confirmed, new DateOnly(2026, 3, 10), 500m, 500m, "BRL", source, dest)
        };

        BalanceCalculator.CalculateConfirmedDelta(transactions, source).Should().Be(-500m, "source loses funds");
        BalanceCalculator.CalculateConfirmedDelta(transactions, dest).Should().Be(500m, "destination gains funds");
    }
}

