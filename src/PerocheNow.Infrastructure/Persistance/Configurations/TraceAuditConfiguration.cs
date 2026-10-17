using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerocheNow.Domain.Audit;

namespace PerocheNow.Infrastructure.Persistance.Configurations;

internal sealed class TraceAuditConfiguration : IEntityTypeConfiguration<TraceAudit>
{
    public void Configure(EntityTypeBuilder<TraceAudit> builder)
    {
        builder.ToTable("TraceAudit");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Action).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Auteur).HasMaxLength(100).IsRequired();
        builder.Property(t => t.TypeObjet).HasMaxLength(100);
        builder.Property(t => t.Details).HasMaxLength(2000);
        builder.Property(t => t.Le).IsRequired();

        builder.HasIndex(t => t.Le);
        builder.HasIndex(t => t.IdObjet);
    }
}
