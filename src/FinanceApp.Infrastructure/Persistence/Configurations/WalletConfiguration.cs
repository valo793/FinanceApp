using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.WalletType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(x => x.RiskProfile).HasMaxLength(20);
        builder.Property(x => x.LockVersion).IsConcurrencyToken();
        builder.HasIndex(x => new { x.UserId, x.WalletType });
        builder.HasIndex(x => new { x.UserId, x.Name }).IsUnique();
    }
}
