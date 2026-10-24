
using Feryal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Feryal.Infrastructure.Persistence.Configurations;

public class ColorConfiguration : IEntityTypeConfiguration<Color>
{
    public void Configure(EntityTypeBuilder<Color> builder)
    {
        builder.ToTable("Colors");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.HexCode)
            .HasMaxLength(20);

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.ProductId, x.Name })
            .IsUnique();

        builder.HasIndex(x => new { x.ProductId, x.Id })
            .IsUnique();

        builder.HasIndex(x => x.ProductId);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.Colors)
            .HasForeignKey(x => x.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.ProductVariants)
            .WithOne(x => x.Color)
            .HasForeignKey(x => new { x.ProductId, x.ColorId })
            .HasPrincipalKey(x => new { x.ProductId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}