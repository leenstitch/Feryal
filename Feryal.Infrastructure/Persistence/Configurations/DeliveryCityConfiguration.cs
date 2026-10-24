using Feryal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Feryal.Infrastructure.Persistence.Configurations;

public class DeliveryCityConfiguration
    : IEntityTypeConfiguration<DeliveryCity>
{
    public void Configure(EntityTypeBuilder<DeliveryCity> builder)
    {
        builder.ToTable("DeliveryCities");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.DeliveryFee)
            .HasPrecision(18, 2);

        builder.HasIndex(x => x.Name)
            .IsUnique();

     
    }
}