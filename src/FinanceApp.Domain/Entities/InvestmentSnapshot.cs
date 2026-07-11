using System;
using FinanceApp.Domain.Common;

namespace FinanceApp.Domain.Entities;

public sealed class InvestmentSnapshot : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid InvestmentId { get; private set; }
    public decimal Value { get; private set; }
    public DateOnly SnapshotDate { get; private set; }

    private InvestmentSnapshot() { }

    public InvestmentSnapshot(Guid userId, Guid investmentId, decimal value, DateOnly snapshotDate)
    {
        UserId = userId;
        InvestmentId = investmentId;
        Value = value;
        SnapshotDate = snapshotDate;
    }
}
