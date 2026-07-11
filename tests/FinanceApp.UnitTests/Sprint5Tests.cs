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
}
