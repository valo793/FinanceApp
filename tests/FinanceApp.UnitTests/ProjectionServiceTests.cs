using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Persistence;
using FinanceApp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinanceApp.UnitTests;

public sealed class ProjectionServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static FinanceDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new FinanceDbContext(options);
    }

    [Fact]
    public async Task GetProjectionAsync_ShouldStartWithCurrentBalance()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        
        var account1 = new Account(UserId, "Conta 1", "checking", "BRL", 1000m, true, true);
        var account2 = new Account(UserId, "Conta 2", "savings", "BRL", 500m, true, true);
        dbContext.Accounts.AddRange(account1, account2);
        await dbContext.SaveChangesAsync();

        var service = new ProjectionService(dbContext);

        // Act
        var result = await service.GetProjectionAsync(UserId, months: 1, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        var todayPoint = result.First();
        todayPoint.ProjectedBalance.Should().Be(1500m); // 1000 + 500
    }

    [Fact]
    public async Task GetProjectionAsync_ShouldIncludePlannedTransactions()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        
        var account = new Account(UserId, "Conta", "checking", "BRL", 1000m, true, true);
        dbContext.Accounts.Add(account);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var tomorrow = today.AddDays(1);
        
        var plannedIncome = new Transaction(
            UserId, 
            TransactionTypes.Income, 
            "Freelance Planejado", 
            TransactionStatuses.Planned, 
            tomorrow, 
            500m, 
            null, 
            "BRL", 
            account.Id
        );
        dbContext.Transactions.Add(plannedIncome);
        await dbContext.SaveChangesAsync();

        var service = new ProjectionService(dbContext);

        // Act
        var result = await service.GetProjectionAsync(UserId, months: 1, CancellationToken.None);

        // Assert
        var tomorrowPoint = result.FirstOrDefault(x => x.Date == tomorrow);
        tomorrowPoint.Should().NotBeNull();
        tomorrowPoint!.ProjectedBalance.Should().Be(1500m); // 1000 + 500
        tomorrowPoint.ProjectedIncome.Should().Be(500m);
    }

    [Fact]
    public async Task GetProjectionAsync_ShouldProjectFutureRecurrenceWithoutDoubleCounting()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        
        var account = new Account(UserId, "Conta", "checking", "BRL", 1000m, true, true);
        dbContext.Accounts.Add(account);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var nextWeek = today.AddDays(7);
        var twoWeeks = today.AddDays(14);

        // Active template: weekly recurring, next run in 7 days
        var template = new RecurringTransaction(
            UserId,
            account.Id,
            "expense",
            "Assinatura Semanal",
            "weekly",
            today,
            nextWeek,
            100m,
            "BRL"
        );
        dbContext.RecurringTransactions.Add(template);

        // One materialized planned transaction already exists in DB for nextWeek
        var existingPlanned = new Transaction(
            UserId,
            TransactionTypes.Expense,
            "Assinatura Semanal - Materializado",
            TransactionStatuses.Planned,
            nextWeek,
            100m,
            null,
            "BRL",
            account.Id,
            recurringTransactionId: template.Id
        );
        dbContext.Transactions.Add(existingPlanned);
        await dbContext.SaveChangesAsync();

        var service = new ProjectionService(dbContext);

        // Act
        var result = await service.GetProjectionAsync(UserId, months: 1, CancellationToken.None);

        // Assert
        // On nextWeek, balance should decrease by 100m (due to the materialized planned transaction)
        var nextWeekPoint = result.FirstOrDefault(x => x.Date == nextWeek);
        nextWeekPoint.Should().NotBeNull();
        nextWeekPoint!.ProjectedBalance.Should().Be(900m); // 1000 - 100 (should not be 800m, i.e., no double counting!)

        // On twoWeeks, balance should decrease by another 100m (due to the simulated template run)
        var twoWeeksPoint = result.FirstOrDefault(x => x.Date == twoWeeks);
        twoWeeksPoint.Should().NotBeNull();
        twoWeeksPoint!.ProjectedBalance.Should().Be(800m); // 900 - 100 (simulated run)
    }
}
