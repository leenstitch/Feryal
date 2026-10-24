using Feryal.Domain.Entities;
using Feryal.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Feryal.Infrastructure.Persistence.Configurations;

public class CouponUsageConfiguration
    : IEntityTypeConfiguration<CouponUsage>
{
    public void Configure(EntityTypeBuilder<CouponUsage> builder)
    {
        builder.ToTable("CouponUsages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DiscountAmount)
            .HasPrecision(18, 2);

        builder.HasIndex(x => x.CouponId);
        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => x.OrderId)
            .IsUnique();

        builder.HasOne(x => x.Coupon)
            .WithMany(x => x.Usages)
            .HasForeignKey(x => x.CouponId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany(x => x.CouponUsages)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}