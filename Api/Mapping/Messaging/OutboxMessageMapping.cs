using Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Mapping.Messaging;

public class OutboxMessageMapping : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessage");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.MessageId).IsUnique();
        builder.HasIndex(x => x.ProcessedAtUtc);

        builder.Property(x => x.Queue).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Payload).IsRequired().HasColumnType("jsonb");
    }
}
