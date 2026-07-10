using FinanceApp.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace FinanceApp.UnitTests;

public sealed class RecurringTransactionTests
{
    [Fact]
    public void Should_advance_monthly_recurring_date()
    {
        var template = new RecurringTransaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "expense",
            "Aluguel",
            "monthly",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 3, 1),
            2000m,
            "BRL");

        template.Advance();

        template.NextRunDate.Should().Be(new DateOnly(2026, 4, 1));
    }
}
