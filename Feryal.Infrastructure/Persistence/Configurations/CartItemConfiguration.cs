using Feryal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Feryal.Infrastructure.Persistence.Configurations;

public sealed class CartItemConfiguration
    : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable(
            "CartItems",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_CartItems_Quantity_Positive",
                    "[Quantity] > 0");
            });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.CartId)
            .IsRequired();

        builder.Property(item => item.ProductVariantId)
            .IsRequired();

        builder.Property(item => item.Quantity)
            .IsRequired();

        builder.HasIndex(item => new
        {
            item.CartId,
            item.ProductVariantId
        })
        .IsUnique();

        builder.HasOne(item => item.Cart)
            .WithMany(cart => cart.Items)
            .HasForeignKey(item => item.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.ProductVariant)
            .WithMany()
            .HasForeignKey(item => item.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}