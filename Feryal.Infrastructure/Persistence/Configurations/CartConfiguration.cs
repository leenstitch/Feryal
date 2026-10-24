using Feryal.Domain.Entities;
using Feryal.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Feryal.Infrastructure.Persistence.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");

        builder.HasKey(cart => cart.Id);

        builder.Property(cart => cart.UserId)
            .IsRequired();

        builder.HasIndex(cart => cart.UserId)
            .IsUnique();

        builder.Property(cart => cart.CreatedAt)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithOne(user => user.Cart)
            .HasForeignKey<Cart>(cart => cart.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(cart => cart.Items)
            .WithOne(item => item.Cart)
            .HasForeignKey(item => item.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(cart => cart.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}