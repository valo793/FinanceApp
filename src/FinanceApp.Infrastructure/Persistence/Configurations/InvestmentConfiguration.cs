using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public sealed class InvestmentConfiguration : IEntityTypeConfiguration<Investment>
{
    public void Configure(EntityTypeBuilder<Investment> builder)
    {
        builder.ToTable("investments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Ticker).HasMaxLength(30);
        builder.Property(x => x.AssetType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(19, 4);
        builder.Property(x => x.AveragePrice).HasPrecision(19, 4);
        builder.Property(x => x.CurrentPrice).HasPrecision(19, 4);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(x => x.RiskLevel).HasMaxLength(20).IsRequired();
        builder.Property(x => x.LockVersion).IsConcurrencyToken();
    }
}
