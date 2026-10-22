using PerocheNow.Domain.Commun;
using PerocheNow.Domain.Plateformes;

namespace PerocheNow.Domain.Tests.Plateformes;

public sealed class AccesInstagramTests
{
    private static readonly DateTimeOffset Maintenant = new(2026, 10, 22, 19, 0, 0, TimeSpan.Zero);

    private static AccesInstagram Acces() =>
        AccesInstagram.Creer(" IGAAjeton ", " peroche ", Maintenant + AccesInstagram.DureeDuJeton, Maintenant);

    [Fact]
    public void Un_acces_cree_garde_le_jeton_et_le_compte_nettoyes()
    {
        var acces = Acces();

        Assert.Equal("IGAAjeton", acces.Jeton);
        Assert.Equal("peroche", acces.Compte);
        Assert.Equal(Maintenant, acces.ObtenuLe);
        Assert.False(acces.EstExpire(Maintenant));
    }

    [Theory]
    [InlineData("")]
    [InlineData("deux mots")]
    public void Un_jeton_vide_ou_en_plusieurs_morceaux_est_refuse(string jeton)
    {
        Assert.Throws<RegleMetierException>(() =>
            AccesInstagram.Creer(jeton, "peroche", Maintenant.AddDays(60), Maintenant));
    }

    [Fact]
    public void Un_jeton_deja_expire_est_refuse()
    {
        Assert.Throws<RegleMetierException>(() =>
            AccesInstagram.Creer("IGAAjeton", "peroche", Maintenant, Maintenant));
    }

    [Fact]
    public void Le_jeton_est_renouvele_au_bout_d_une_semaine_tant_qu_il_est_valide()
    {
        var acces = Acces();

        Assert.False(acces.DoitEtreRenouvele(Maintenant.AddDays(6)));
        Assert.True(acces.DoitEtreRenouvele(Maintenant.AddDays(7)));
        Assert.False(acces.DoitEtreRenouvele(Maintenant.AddDays(60)));
        Assert.True(acces.EstExpire(Maintenant.AddDays(60)));
    }

    [Fact]
    public void Un_renouvellement_repart_de_zero_pour_le_meme_compte()
    {
        var acces = Acces();
        var plusTard = Maintenant.AddDays(8);

        acces.Renouveler("IGAAnouveau", plusTard.AddDays(60), plusTard);

        Assert.Equal("IGAAnouveau", acces.Jeton);
        Assert.Equal("peroche", acces.Compte);
        Assert.Equal(plusTard, acces.ObtenuLe);
        Assert.False(acces.DoitEtreRenouvele(plusTard.AddDays(1)));
    }
}
