using Feryal.Domain.Entities;
using Feryal.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Feryal.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration
    : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.Type)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.UserId,
            x.IsRead
        });

        builder.HasIndex(x => x.RelatedOrderId);

        builder.HasOne<ApplicationUser>()
            .WithMany(x => x.Notifications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(x => x.RelatedOrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}