using PerocheNow.Business.CopiesDeLecture;
using PerocheNow.Business.Textes;
using PerocheNow.Domain.Commun;
using PerocheNow.Domain.Textes;

namespace PerocheNow.Business.Tests.Textes;

public sealed class TextesDuSiteTests
{
    [Fact]
    public async Task Un_texte_saisi_remplace_l_origine_et_la_copie_suit_apres_la_base()
    {
        using var banc = new Banc();
        using var portee = banc.NouvellePortee();

        var restants = await Banc.Mediateur(portee).EnvoyerAsync(new ModifierTextesDuSite(
            [new TexteSaisi("accueil.bouton", "  Rejoindre l'aventure  "), new TexteSaisi("accueil.lien", null)]));

        Assert.Equal(1, restants);
        var texte = Assert.Single(banc.Textes.Textes);
        Assert.Equal("Rejoindre l'aventure", texte.Valeur);
        Assert.Equal(Banc.Instant, texte.ModifieLe);

        var (nom, enregistrementsAvant) = Assert.Single(banc.Copies.Publications);
        Assert.Equal(NomsDesCopies.TextesDuSite, nom);
        Assert.Equal(1, enregistrementsAvant);
        Assert.Equal("Rejoindre l'aventure", banc.Copies.Copie<TextesPublies>(NomsDesCopies.TextesDuSite)?.Textes["accueil.bouton"]);
    }

    [Fact]
    public async Task Vider_un_texte_rend_le_texte_d_origine()
    {
        using var banc = new Banc();
        banc.Textes.Textes.Add(TexteDuSite.Creer("accueil.bouton", "Ancien", Banc.Instant));
        banc.Textes.Textes.Add(TexteDuSite.Creer("pied.devise", "Garder", Banc.Instant));
        using var portee = banc.NouvellePortee();

        await Banc.Mediateur(portee).EnvoyerAsync(new ModifierTextesDuSite([new TexteSaisi("accueil.bouton", "   ")]));

        Assert.Equal("pied.devise", Assert.Single(banc.Textes.Textes).Cle);
        var copie = Assert.IsType<TextesPublies>(banc.Copies.Copie<TextesPublies>(NomsDesCopies.TextesDuSite));
        Assert.False(copie.Textes.ContainsKey("accueil.bouton"));
        Assert.Equal("Garder", copie.Textes["pied.devise"]);
    }

    [Fact]
    public async Task Modifier_un_texte_existant_garde_son_identite()
    {
        using var banc = new Banc();
        var existant = TexteDuSite.Creer("accueil.bouton", "Ancien", Banc.Instant.AddDays(-3));
        banc.Textes.Textes.Add(existant);
        using var portee = banc.NouvellePortee();

        await Banc.Mediateur(portee).EnvoyerAsync(new ModifierTextesDuSite([new TexteSaisi("accueil.bouton", "Nouveau")]));

        Assert.Same(existant, Assert.Single(banc.Textes.Textes));
        Assert.Equal("Nouveau", existant.Valeur);
        Assert.Equal(Banc.Instant, existant.ModifieLe);
    }

    [Fact]
    public async Task Un_texte_trop_long_n_enregistre_rien()
    {
        using var banc = new Banc();
        using var portee = banc.NouvellePortee();

        await Assert.ThrowsAsync<RegleMetierException>(() => Banc.Mediateur(portee).EnvoyerAsync(new ModifierTextesDuSite(
            [new TexteSaisi("accueil.bouton", new string('x', TexteDuSite.LongueurMaxValeur + 1))])));

        Assert.Equal(0, banc.UniteDeTravail.Enregistrements);
        Assert.Empty(banc.Copies.Publications);
    }

    [Fact]
    public async Task Les_pages_publiques_lisent_la_copie_et_ne_reconstruisent_qu_une_fois()
    {
        using var banc = new Banc();
        banc.Textes.Textes.Add(TexteDuSite.Creer("accueil.bouton", "Depuis la base", Banc.Instant));

        for (var visite = 0; visite < 3; visite++)
        {
            using var portee = banc.NouvellePortee();
            var textes = await Banc.Mediateur(portee).EnvoyerAsync(new ObtenirTextesPublies());
            Assert.Equal("Depuis la base", textes.Textes["accueil.bouton"]);
        }

        Assert.Equal(1, banc.Textes.Lectures);
    }

    [Fact]
    public async Task Sans_aucun_texte_modifie_la_copie_vide_evite_aussi_la_base()
    {
        using var banc = new Banc();

        for (var visite = 0; visite < 2; visite++)
        {
            using var portee = banc.NouvellePortee();
            Assert.Empty((await Banc.Mediateur(portee).EnvoyerAsync(new ObtenirTextesPublies())).Textes);
        }

        Assert.Equal(1, banc.Textes.Lectures);
    }
}
