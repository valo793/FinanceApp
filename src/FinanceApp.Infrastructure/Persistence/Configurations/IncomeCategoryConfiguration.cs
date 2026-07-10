using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public sealed class IncomeCategoryConfiguration : IEntityTypeConfiguration<IncomeCategory>
{
    public void Configure(EntityTypeBuilder<IncomeCategory> builder)
    {
        builder.ToTable("income_categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(20);
        builder.Property(x => x.Icon).HasMaxLength(50);
        builder.Property(x => x.LockVersion).IsConcurrencyToken();
        builder.HasIndex(x => new { x.UserId, x.Name }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.ParentCategoryId });
    }
}
