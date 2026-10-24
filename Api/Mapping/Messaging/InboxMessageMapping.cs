using Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Mapping.Messaging;

public class InboxMessageMapping : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessage");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.MessageId).IsUnique();
    }
}
