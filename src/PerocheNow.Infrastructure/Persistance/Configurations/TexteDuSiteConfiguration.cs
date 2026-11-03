using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerocheNow.Domain.Textes;

namespace PerocheNow.Infrastructure.Persistance.Configurations;

internal sealed class TexteDuSiteConfiguration : IEntityTypeConfiguration<TexteDuSite>
{
    public void Configure(EntityTypeBuilder<TexteDuSite> builder)
    {
        builder.ToTable("TexteDuSite");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Cle).HasMaxLength(TexteDuSite.LongueurMaxCle).IsRequired();
        builder.HasIndex(t => t.Cle).IsUnique();
        builder.Property(t => t.Valeur).HasMaxLength(TexteDuSite.LongueurMaxValeur).IsRequired();
        builder.Property(t => t.ModifieLe).IsRequired();
    }
}
