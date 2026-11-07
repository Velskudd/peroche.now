using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerocheNow.Domain.Medias;

namespace PerocheNow.Infrastructure.Persistance.Configurations;

internal sealed class MediaDuSiteConfiguration : IEntityTypeConfiguration<MediaDuSite>
{
    public void Configure(EntityTypeBuilder<MediaDuSite> builder)
    {
        builder.ToTable("MediaDuSite");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Cle).HasMaxLength(MediaDuSite.LongueurMaxCle).IsRequired();
        builder.HasIndex(m => m.Cle).IsUnique();
        builder.Property(m => m.Fichier).HasMaxLength(MediaDuSite.LongueurMaxFichier).IsRequired();
        builder.Property(m => m.TypeContenu).HasMaxLength(50).IsRequired();
        builder.Property(m => m.Taille).IsRequired();
        builder.Property(m => m.ModifieLe).IsRequired();
    }
}
