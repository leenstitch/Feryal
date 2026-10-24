using Feryal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Feryal.Infrastructure.Persistence.Configurations;

public class ProductImageConfiguration
    : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");

        builder.HasKey(image => image.Id);

        builder.Property(image => image.ImageUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(image => image.PublicId)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(image => new
        {
            image.ProductId,
            image.DisplayOrder
        });

        builder.HasIndex(image => image.ProductId)
            .HasFilter("[IsPrimary] = 1")
            .IsUnique();

        builder.HasOne(image => image.Product)
            .WithMany(product => product.Images)
            .HasForeignKey(image => image.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}