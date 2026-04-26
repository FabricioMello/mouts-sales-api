using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(m => m.EventName).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Payload).IsRequired().HasColumnType("text");
        builder.Property(m => m.OccurredAt).IsRequired();
        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.ProcessedAt);
        builder.Property(m => m.RetryCount).IsRequired().HasDefaultValue(0);
        builder.Property(m => m.LastError).HasMaxLength(2000);

        builder.HasIndex(m => m.ProcessedAt)
            .HasFilter("\"ProcessedAt\" IS NULL")
            .HasDatabaseName("IX_OutboxMessages_Pending");

        builder.HasIndex(m => m.CreatedAt);
    }
}
