using System;

namespace FinanceApp.Contracts.Snapshots;

public sealed class BalanceSnapshotDto
{
    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public decimal Balance { get; init; }
    public DateOnly SnapshotDate { get; init; }
}
