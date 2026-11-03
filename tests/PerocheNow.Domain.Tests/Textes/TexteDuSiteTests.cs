using PerocheNow.Domain.Commun;
using PerocheNow.Domain.Textes;

namespace PerocheNow.Domain.Tests.Textes;

public sealed class TexteDuSiteTests
{
    private static readonly DateTimeOffset Maintenant = new(2026, 11, 3, 21, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Un_texte_garde_son_repere_et_sa_valeur_nettoyes()
    {
        var texte = TexteDuSite.Creer(" accueil.bouton ", "  Suivre le projet  ", Maintenant);

        Assert.Equal("accueil.bouton", texte.Cle);
        Assert.Equal("Suivre le projet", texte.Valeur);
        Assert.Equal(Maintenant, texte.ModifieLe);
    }

    [Theory]
    [InlineData("", "Texte")]
    [InlineData("accueil.bouton", " ")]
    public void Un_repere_ou_un_texte_vide_est_refuse(string cle, string valeur)
    {
        Assert.Throws<RegleMetierException>(() => TexteDuSite.Creer(cle, valeur, Maintenant));
    }

    [Fact]
    public void Un_repere_trop_long_est_refuse()
    {
        Assert.Throws<RegleMetierException>(() =>
            TexteDuSite.Creer(new string('a', TexteDuSite.LongueurMaxCle + 1), "Texte", Maintenant));
    }
}
