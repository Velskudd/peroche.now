using PerocheNow.Domain.Commun;
using PerocheNow.Domain.Maintenant;

namespace PerocheNow.Domain.Tests.Maintenant;

public sealed class ObjectifTests
{
    [Fact]
    public void Sans_date_l_objectif_n_est_pas_confirme()
    {
        var objectif = new Objectif("  Test Week Nordschleife ");

        Assert.Equal("Test Week Nordschleife", objectif.Libelle);
        Assert.False(objectif.DateConfirmee);
    }

    [Fact]
    public void Avec_une_date_l_objectif_est_confirme()
    {
        var objectif = new Objectif("Test Week Nordschleife", new DateOnly(2027, 3, 15));

        Assert.True(objectif.DateConfirmee);
        Assert.Equal(new DateOnly(2027, 3, 15), objectif.DateCible);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Un_libelle_vide_est_refuse(string libelle)
    {
        Assert.Throws<RegleMetierException>(() => new Objectif(libelle));
    }

    [Fact]
    public void Un_libelle_trop_long_est_refuse()
    {
        Assert.Throws<RegleMetierException>(() => new Objectif(new string('a', Objectif.LongueurMaxLibelle + 1)));
    }
}
