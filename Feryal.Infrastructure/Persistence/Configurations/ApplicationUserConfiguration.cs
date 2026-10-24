using Feryal.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Feryal.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("AspNetUsers");

        builder.Property(x => x.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Area)
            .HasMaxLength(150);

        builder.Property(x => x.Street)
            .HasMaxLength(200);

        builder.Property(x => x.BuildingNumber)
            .HasMaxLength(50);

        builder.Property(x => x.AdditionalDetails)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.TokenVersion)
            .IsRequired();

        builder.HasIndex(x => x.NormalizedEmail)
            .IsUnique();

        builder.HasOne(x => x.DeliveryCity)
            .WithMany()
            .HasForeignKey(x => x.DeliveryCityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}