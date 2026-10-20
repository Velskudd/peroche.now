using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerocheNow.Domain.Audit;

namespace PerocheNow.Infrastructure.Persistence.Configurations;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntry");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Action).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Author).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ObjectType).HasMaxLength(100);
        builder.Property(e => e.Details).HasMaxLength(2000);
        builder.Property(e => e.OccurredAt).IsRequired();

        builder.HasIndex(e => e.OccurredAt);
        builder.HasIndex(e => e.ObjectId);
    }
}
