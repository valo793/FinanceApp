using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public sealed class BalanceSnapshotConfiguration : IEntityTypeConfiguration<BalanceSnapshot>
{
    public void Configure(EntityTypeBuilder<BalanceSnapshot> builder)
    {
        builder.ToTable("balance_snapshots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Balance).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.SnapshotDate).IsRequired();
        builder.Property(x => x.LockVersion).IsConcurrencyToken();
        builder.HasIndex(x => new { x.UserId, x.AccountId, x.SnapshotDate }).IsUnique();
    }
}
