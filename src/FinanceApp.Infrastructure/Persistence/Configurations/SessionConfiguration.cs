using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RefreshTokenHash).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DeviceName).HasMaxLength(120);
        builder.Property(x => x.IpHash).HasMaxLength(128);
        builder.HasIndex(x => new { x.UserId, x.ExpiresAt });
        builder.HasIndex(x => x.RefreshTokenFamilyId);
    }
}
