using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public sealed class InvestmentSnapshotConfiguration : IEntityTypeConfiguration<InvestmentSnapshot>
{
    public void Configure(EntityTypeBuilder<InvestmentSnapshot> builder)
    {
        builder.ToTable("investment_snapshots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Value).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.SnapshotDate).IsRequired();
        builder.Property(x => x.LockVersion).IsConcurrencyToken();
        builder.HasIndex(x => new { x.UserId, x.InvestmentId, x.SnapshotDate }).IsUnique();
    }
}
