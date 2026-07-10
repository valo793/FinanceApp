using FinanceApp.Domain.Entities;
using FluentAssertions;
using Xunit;
using System;

namespace FinanceApp.UnitTests;

public sealed class AccountTests
{
    [Fact]
    public void Should_update_account_active_status()
    {
        var account = new Account(
            Guid.NewGuid(),
            "Conta Corrente",
            "checking",
            "BRL",
            1000m,
            true,
            true);

        account.IsActive.Should().BeTrue();

        account.Update(
            "Conta Corrente Atualizada",
            "checking",
            "BRL",
            1000m,
            true,
            true,
            isActive: false);

        account.IsActive.Should().BeFalse();
        account.Name.Should().Be("Conta Corrente Atualizada");
    }
}
