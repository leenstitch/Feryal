
using Feryal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Feryal.Infrastructure.Persistence.Configurations;

public sealed class PickupPointConfiguration
    : IEntityTypeConfiguration<PickupPoint>
{
    public void Configure(EntityTypeBuilder<PickupPoint> builder)
    {
        builder.ToTable("PickupPoints");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.PickupFee)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.IsActive);
    }
}
