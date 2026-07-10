using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.AccountType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(x => x.OpeningBalance).HasPrecision(19, 4);
        builder.Property(x => x.CurrentBalanceCached).HasPrecision(19, 4);
        builder.Property(x => x.LockVersion).IsConcurrencyToken();
        builder.HasIndex(x => new { x.UserId, x.IsActive });
    }
}
