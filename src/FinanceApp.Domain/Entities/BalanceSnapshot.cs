using System;
using FinanceApp.Domain.Common;

namespace FinanceApp.Domain.Entities;

public sealed class BalanceSnapshot : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid AccountId { get; private set; }
    public decimal Balance { get; private set; }
    public DateOnly SnapshotDate { get; private set; }

    private BalanceSnapshot() { }

    public BalanceSnapshot(Guid userId, Guid accountId, decimal balance, DateOnly snapshotDate)
    {
        UserId = userId;
        AccountId = accountId;
        Balance = balance;
        SnapshotDate = snapshotDate;
    }
}
