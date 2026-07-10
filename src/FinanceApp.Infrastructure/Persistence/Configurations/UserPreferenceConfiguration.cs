using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public sealed class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        builder.ToTable("user_preferences");
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.Theme).HasMaxLength(20).IsRequired();
        builder.Property(x => x.AccentColor).HasMaxLength(20);
        builder.Property(x => x.Density).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DefaultDashboardPeriod).HasMaxLength(20).IsRequired();
    }
}
