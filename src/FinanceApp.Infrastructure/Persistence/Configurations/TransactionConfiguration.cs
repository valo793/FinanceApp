using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TransactionType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(300).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(x => x.AmountExpected).HasPrecision(19, 4);
        builder.Property(x => x.AmountActual).HasPrecision(19, 4);
        builder.Property(x => x.InvestmentQuantity).HasPrecision(19, 4);
        builder.Property(x => x.UnitPrice).HasPrecision(19, 4);
        builder.Property(x => x.LockVersion).IsConcurrencyToken();
        builder.HasIndex(x => x.InvestmentId);
        builder.HasIndex(x => new { x.UserId, x.CompetenceDate });
        builder.HasIndex(x => new { x.UserId, x.AccountId, x.CompetenceDate });
    }
}
