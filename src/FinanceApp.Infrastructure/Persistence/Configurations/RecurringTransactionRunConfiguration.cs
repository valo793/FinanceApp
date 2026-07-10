using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public sealed class RecurringTransactionRunConfiguration : IEntityTypeConfiguration<RecurringTransactionRun>
{
    public void Configure(EntityTypeBuilder<RecurringTransactionRun> builder)
    {
        builder.ToTable("recurring_transaction_runs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(300);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => new { x.RecurringTransactionId, x.ScheduledFor }).IsUnique();
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
    }
}
