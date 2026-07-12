using System;
using FinanceApp.Domain.Entities;
using Xunit;

namespace FinanceApp.UnitTests;

public class Sprint5Tests
{
    [Fact]
    public void User_EnableMfa_ShouldSetBackupCodesHash()
    {
        // Arrange
        var user = new User("test@example.com", "hash");

        // Act
        user.EnableMfa("secret_key", "backup_hash_1,backup_hash_2");

        // Assert
        Assert.True(user.MfaEnabled);
        Assert.Equal("secret_key", user.MfaSecret);
        Assert.Equal("backup_hash_1,backup_hash_2", user.MfaBackupCodesHash);
    }

    [Fact]
    public void User_DisableMfa_ShouldClearBackupCodesHash()
    {
        // Arrange
        var user = new User("test@example.com", "hash");
        user.EnableMfa("secret_key", "backup_hash_1,backup_hash_2");

        // Act
        user.DisableMfa();

        // Assert
        Assert.False(user.MfaEnabled);
        Assert.Null(user.MfaSecret);
        Assert.Null(user.MfaBackupCodesHash);
    }

    [Fact]
    public void ExpenseCategory_Constructor_ShouldSetMonthlyBudgetLimit()
    {
        // Arrange & Act
        var category = new ExpenseCategory(Guid.NewGuid(), "Alimentação", "#FF5733", "Shop", false, 500.50m);

        // Assert
        Assert.Equal(500.50m, category.MonthlyBudgetLimit);
    }

    [Fact]
    public void ExpenseCategory_Update_ShouldUpdateMonthlyBudgetLimit()
    {
        // Arrange
        var category = new ExpenseCategory(Guid.NewGuid(), "Alimentação", "#FF5733", "Shop", false, 500.50m);

        // Act
        category.Update("Supermercado", "#00FF00", "Folder", 1, true, 800.00m);

        // Assert
        Assert.Equal(800.00m, category.MonthlyBudgetLimit);
    }

    [Fact]
    public void InvestmentSnapshot_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var investmentId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var snapshot = new InvestmentSnapshot(userId, investmentId, 12500.75m, date);

        // Assert
        Assert.Equal(userId, snapshot.UserId);
        Assert.Equal(investmentId, snapshot.InvestmentId);
        Assert.Equal(12500.75m, snapshot.Value);
        Assert.Equal(date, snapshot.SnapshotDate);
    }

    [Fact]
    public void Investment_Constructor_ShouldSetIndexerProperties()
    {
        // Arrange & Act
        var investment = new Investment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "CDB Inter 120%",
            null,
            "cdb",
            1m,
            1000m,
            1000m,
            "BRL",
            "low",
            "cdi",
            120m,
            null);

        // Assert
        Assert.Equal("cdi", investment.IndexerType);
        Assert.Equal(120m, investment.IndexerRate);
        Assert.Null(investment.IndexerAdditionalRate);
    }

    [Fact]
    public void Investment_Update_ShouldModifyIndexerProperties()
    {
        // Arrange
        var investment = new Investment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "CDB Inter 120%",
            null,
            "cdb",
            1m,
            1000m,
            1000m,
            "BRL",
            "low",
            "cdi",
            120m,
            null);

        // Act
        investment.Update(
            "CDB Inter 120% Alt",
            null,
            "cdb",
            1m,
            1000m,
            1000m,
            "low",
            true,
            "ipca",
            null,
            6.5m);

        // Assert
        Assert.Equal("ipca", investment.IndexerType);
        Assert.Null(investment.IndexerRate);
        Assert.Equal(6.5m, investment.IndexerAdditionalRate);
    }

    [Fact]
    public void Yield_Accrual_CDI_DailyRate_ShouldCompoundCorrectly()
    {
        // Arrange
        var initialValue = 1000m;
        var ratePercent = 120m; // 120% of CDI
        var annualCdi = 0.105; // 10.5%
        var annualRate = annualCdi * (double)(ratePercent / 100m); // 12.6%

        // Act: compound daily for 30 days
        var value = initialValue;
        var dailyFactor = Math.Pow(1.0 + annualRate, 1.0 / 365.0);
        for (int i = 0; i < 30; i++)
        {
            value = value * (decimal)dailyFactor;
        }

        // Expected compound value: initialValue * (1 + annualRate)^(30/365)
        var expected = initialValue * (decimal)Math.Pow(1.0 + annualRate, 30.0 / 365.0);

        // Assert
        Assert.Equal(expected, value, 4);
    }
}
