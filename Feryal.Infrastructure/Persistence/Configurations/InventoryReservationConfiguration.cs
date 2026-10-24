using Feryal.Domain.Entities;
using Feryal.Domain.Enums;
using Feryal.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Feryal.Infrastructure.Persistence.Configurations;

public class InventoryReservationConfiguration
    : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable("InventoryReservations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReservationToken)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.ReservationToken)
            .IsUnique();

        builder.HasIndex(x => new { x.UserId, x.IdempotencyKey })
            .IsUnique();

        builder.HasIndex(x => new { x.Status, x.ExpiresAt });

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_InventoryReservations_UserId");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UserId)
    .IsUnique()
    .HasFilter("[Status] = 1")
    .HasDatabaseName("UX_InventoryReservations_Active_UserId");
        builder.HasIndex(x => x.ReservationToken)
    .IsUnique();

        builder.HasIndex(x => new
        {
            x.UserId,
            x.IdempotencyKey
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.Status,
            x.ExpiresAt
        });

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("[Status] = 1")
            .HasDatabaseName("UX_InventoryReservations_Active_UserId");
    }
}