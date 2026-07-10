using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public sealed class RecurringTransactionConfiguration : IEntityTypeConfiguration<RecurringTransaction>
{
    public void Configure(EntityTypeBuilder<RecurringTransaction> builder)
    {
        builder.ToTable("recurring_transactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TransactionKind).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Frequency).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DefaultAmount).HasPrecision(19, 4);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(x => x.LockVersion).IsConcurrencyToken();
        builder.HasIndex(x => new { x.UserId, x.NextRunDate });
    }
}
