using Feryal.Domain.Entities;
using Feryal.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Feryal.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CustomerName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CustomerPhoneNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.OrderNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.OrderNumber)
            .IsUnique()
            .HasDatabaseName("UX_Orders_OrderNumber");

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.UserId,
            x.IdempotencyKey
        })
        .IsUnique()
        .HasDatabaseName("UX_Orders_UserId_IdempotencyKey");

        builder.Property(x => x.DeliveryCityName)
            .HasMaxLength(100);

        builder.Property(x => x.DeliveryArea)
            .HasMaxLength(100);

        builder.Property(x => x.DeliveryStreet)
            .HasMaxLength(200);

        builder.Property(x => x.DeliveryBuildingNumber)
            .HasMaxLength(50);

        builder.Property(x => x.DeliveryAdditionalDetails)
            .HasMaxLength(1000);

        builder.Property(x => x.PickupPointName)
            .HasMaxLength(150);

        builder.Property(x => x.PickupPointAddress)
            .HasMaxLength(500);

        builder.Property(x => x.Subtotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.DiscountAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.DeliveryFee)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.DeliveryType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.OrderDate).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_Orders_UserId");

        builder.HasIndex(x => x.CouponId)
            .HasDatabaseName("IX_Orders_CouponId");

        builder.HasIndex(x => x.DeliveryCityId)
            .HasDatabaseName("IX_Orders_DeliveryCityId");

        builder.HasIndex(x => x.PickupPointId)
            .HasDatabaseName("IX_Orders_PickupPointId");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_Orders_Status");

        builder.HasIndex(x => x.OrderDate)
            .HasDatabaseName("IX_Orders_OrderDate");

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Coupon>()
            .WithMany()
            .HasForeignKey(x => x.CouponId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DeliveryCity>()
            .WithMany()
            .HasForeignKey(x => x.DeliveryCityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PickupPoint>()
            .WithMany()
            .HasForeignKey(x => x.PickupPointId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}