
using Feryal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Feryal.Infrastructure.Persistence.Configurations;

public class SizeConfiguration : IEntityTypeConfiguration<Size>
{
    public void Configure(EntityTypeBuilder<Size> builder)
    {
        builder.ToTable("Sizes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

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
            .WithMany(x => x.Sizes)
            .HasForeignKey(x => x.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.ProductVariants)
            .WithOne(x => x.Size)
            .HasForeignKey(x => new { x.ProductId, x.SizeId })
            .HasPrincipalKey(x => new { x.ProductId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}