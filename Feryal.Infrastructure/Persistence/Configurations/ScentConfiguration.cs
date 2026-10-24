
using Feryal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Feryal.Infrastructure.Persistence.Configurations;

public class ScentConfiguration : IEntityTypeConfiguration<Scent>
{
    public void Configure(EntityTypeBuilder<Scent> builder)
    {
        builder.ToTable("Scents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

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
            .WithMany(x => x.Scents)
            .HasForeignKey(x => x.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.ProductVariants)
            .WithOne(x => x.Scent)
            .HasForeignKey(x => new { x.ProductId, x.ScentId })
            .HasPrincipalKey(x => new { x.ProductId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}