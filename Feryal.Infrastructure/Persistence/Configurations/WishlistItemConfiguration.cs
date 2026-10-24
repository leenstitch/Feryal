using Feryal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Feryal.Infrastructure.Persistence.Configurations;

public sealed class WishlistItemConfiguration
    : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("WishlistItems");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new
        {
            x.WishlistId,
            x.ProductVariantId
        })
        .IsUnique();

        builder.Property(x => x.AddedAt)
            .IsRequired();

        builder.HasOne(x => x.Wishlist)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.WishlistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}