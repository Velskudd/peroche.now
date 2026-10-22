using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerocheNow.Domain.Plateformes;

namespace PerocheNow.Infrastructure.Persistance.Configurations;

internal sealed class AccesInstagramConfiguration : IEntityTypeConfiguration<AccesInstagram>
{
    public void Configure(EntityTypeBuilder<AccesInstagram> builder)
    {
        builder.ToTable("AccesInstagram");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Jeton).HasMaxLength(AccesInstagram.LongueurMaxJeton).IsRequired();
        builder.Property(a => a.Compte).HasMaxLength(AccesInstagram.LongueurMaxCompte).IsRequired();
        builder.Property(a => a.ObtenuLe).IsRequired();
        builder.Property(a => a.ExpireLe).IsRequired();
    }
}
