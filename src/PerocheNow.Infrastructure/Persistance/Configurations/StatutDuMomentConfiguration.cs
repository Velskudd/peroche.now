using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerocheNow.Domain.Maintenant;

namespace PerocheNow.Infrastructure.Persistance.Configurations;

internal sealed class StatutDuMomentConfiguration : IEntityTypeConfiguration<StatutDuMoment>
{
    public void Configure(EntityTypeBuilder<StatutDuMoment> builder)
    {
        builder.ToTable("StatutDuMoment");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Accroche)
            .HasMaxLength(StatutDuMoment.LongueurMaxAccroche)
            .IsRequired();

        builder.Property(s => s.MisAJourLe).IsRequired();

        builder.OwnsMany(s => s.Lignes, lignes =>
        {
            lignes.ToTable("LigneDuMoment");
            lignes.WithOwner().HasForeignKey("StatutDuMomentId");
            lignes.Property<int>("Id");
            lignes.HasKey("Id");
            lignes.Property(l => l.Ordre).IsRequired();
            lignes.Property(l => l.Texte)
                .HasMaxLength(StatutDuMoment.LongueurMaxLigne)
                .IsRequired();
            // Stockée en clair (« ProjetEnCours », « Contenus »…) pour rester lisible en base.
            lignes.Property(l => l.Rubrique)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
        });

        // La collection est exposée en lecture seule : EF passe par le champ privé _lignes.
        builder.Navigation(s => s.Lignes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsOne(s => s.ProchainObjectif, objectif =>
        {
            objectif.Property(o => o.Libelle)
                .HasColumnName("ObjectifLibelle")
                .HasMaxLength(Objectif.LongueurMaxLibelle)
                .IsRequired();
            objectif.Property(o => o.DateCible).HasColumnName("ObjectifDateCible");
            objectif.Ignore(o => o.DateConfirmee);
        });
    }
}
