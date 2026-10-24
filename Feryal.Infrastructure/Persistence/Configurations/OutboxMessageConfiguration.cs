
using Feryal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Feryal.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration
    : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(
        EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.EventType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(message => message.Payload)
            .IsRequired();

        builder.Property(message => message.CreatedAt)
            .IsRequired();

        builder.Property(message => message.ProcessedAt)
            .IsRequired(false);

        builder.Property(message => message.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(message => message.Error)
            .IsRequired(false)
            .HasMaxLength(2000);

        builder.HasIndex(message => new
        {
            message.ProcessedAt,
            message.CreatedAt
        })
            .HasDatabaseName(
                "IX_OutboxMessages_ProcessedAt_CreatedAt");
    }
}