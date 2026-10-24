using Feryal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Feryal.Infrastructure.Persistence.Configurations;

public class InventoryReservationItemConfiguration
    : IEntityTypeConfiguration<InventoryReservationItem>
{
    public void Configure(
        EntityTypeBuilder<InventoryReservationItem> builder)
    {
        builder.ToTable("InventoryReservationItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.ReservationId,
            x.ProductVariantId
        }).IsUnique();

        builder.HasOne(x => x.Reservation)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_InventoryReservationItems_Quantity",
                "[Quantity] > 0");
        });
    }
}