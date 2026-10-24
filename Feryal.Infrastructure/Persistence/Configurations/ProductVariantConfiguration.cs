using Feryal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Feryal.Infrastructure.Persistence.Configurations;

public sealed class ProductVariantConfiguration
    : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.HasKey(variant => variant.Id);

        builder.Property(variant => variant.SKU)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(variant => variant.Description)
            .HasMaxLength(2000);

        builder.Property(variant => variant.Price)
            .HasPrecision(18, 2);

        builder.Property(variant => variant.DiscountPercentage)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(variant => variant.StockQuantity);

        builder.Property(variant => variant.IsActive)
            .IsRequired();

        builder.Property(variant => variant.IsConfigured)
            .IsRequired();

        builder.Property(variant => variant.CreatedAt)
            .IsRequired();

        builder.HasIndex(variant => variant.SKU)
            .IsUnique();

        builder.HasIndex(variant => variant.ProductId);

        builder.HasIndex(variant => new
        {
            variant.ProductId,
            variant.IsActive,
            variant.IsConfigured
        });

        builder.HasIndex(variant => new
        {
            variant.ProductId,
            variant.ColorId,
            variant.SizeId,
            variant.ScentId
        }).IsUnique();

        builder.HasOne(variant => variant.Product)
            .WithMany(product => product.Variants)
            .HasForeignKey(variant => variant.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(variant => variant.Color)
            .WithMany(color => color.ProductVariants)
            .HasForeignKey(variant => new
            {
                variant.ProductId,
                variant.ColorId
            })
            .HasPrincipalKey(color => new
            {
                color.ProductId,
                color.Id
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(variant => variant.Size)
            .WithMany(size => size.ProductVariants)
            .HasForeignKey(variant => new
            {
                variant.ProductId,
                variant.SizeId
            })
            .HasPrincipalKey(size => new
            {
                size.ProductId,
                size.Id
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(variant => variant.Scent)
            .WithMany(scent => scent.ProductVariants)
            .HasForeignKey(variant => new
            {
                variant.ProductId,
                variant.ScentId
            })
            .HasPrincipalKey(scent => new
            {
                scent.ProductId,
                scent.Id
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.ReservedQuantity)
    .IsRequired()
    .HasDefaultValue(0);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_ProductVariants_StockQuantity_NonNegative",
                "[StockQuantity] IS NULL OR [StockQuantity] >= 0");

            table.HasCheckConstraint(
                "CK_ProductVariants_ReservedQuantity_NonNegative",
                "[ReservedQuantity] >= 0");

            table.HasCheckConstraint(
                "CK_ProductVariants_ReservedQuantity_NotAboveStock",
                "[StockQuantity] IS NULL OR [ReservedQuantity] <= [StockQuantity]");
        });
    }
}