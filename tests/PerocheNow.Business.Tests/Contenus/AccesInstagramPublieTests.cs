using PerocheNow.Business.Contenus.Instagram;
using PerocheNow.Business.CopiesDeLecture;
using PerocheNow.Domain.Plateformes;

namespace PerocheNow.Business.Tests.Contenus;

/// <summary>La copie de l'accès Instagram : de quoi lire les publications et renouveler le jeton sans réveiller la base.</summary>
public sealed class AccesInstagramPublieTests
{
    [Fact]
    public async Task Brancher_le_compte_publie_la_copie_apres_la_base()
    {
        using var banc = new Banc();
        using var portee = banc.NouvellePortee();

        await Banc.Mediateur(portee).EnvoyerAsync(new ConnecterInstagram(FausseApiInstagram.JetonValide));

        var (nom, enregistrementsAvant) = Assert.Single(banc.Copies.Publications);
        Assert.Equal(NomsDesCopies.AccesInstagram, nom);
        Assert.Equal(1, enregistrementsAvant);
        var copie = banc.Copies.Copie<AccesInstagramPublie>(NomsDesCopies.AccesInstagram)?.Acces;
        Assert.Equal(FausseApiInstagram.JetonValide, copie?.Jeton);
        Assert.Equal(Banc.Instant + AccesInstagram.DureeDuJeton, copie?.ExpireLe);
    }

    [Fact]
    public async Task Le_renouvellement_republie_la_copie_avec_le_nouveau_jeton()
    {
        using var banc = new Banc();
        banc.AccesInstagram.Acces = AccesInstagram.Creer("IGAAancien", "peroche", Banc.Instant.AddDays(50), Banc.Instant.AddDays(-10));
        using var portee = banc.NouvellePortee();

        await Banc.Mediateur(portee).EnvoyerAsync(new RenouvelerAccesInstagram());

        Assert.Equal("IGAArenouvele1", banc.Copies.Copie<AccesInstagramPublie>(NomsDesCopies.AccesInstagram)?.Acces?.Jeton);
    }

    [Fact]
    public async Task Sans_compte_branche_la_copie_le_dit_et_la_base_n_est_lue_qu_une_fois()
    {
        using var banc = new Banc();

        for (var visite = 0; visite < 3; visite++)
        {
            using var portee = banc.NouvellePortee();
            Assert.Null(await Banc.Mediateur(portee).EnvoyerAsync(new ObtenirAccesInstagramPublie()));
        }

        Assert.Equal(1, banc.AccesInstagram.Lectures);
        Assert.Null(Assert.IsType<AccesInstagramPublie>(banc.Copies.Copie<AccesInstagramPublie>(NomsDesCopies.AccesInstagram)).Acces);
    }

    [Fact]
    public void La_copie_applique_les_memes_regles_que_l_acces()
    {
        var obtenuLe = Banc.Instant.AddDays(-8);
        var copie = new CopieAccesInstagram("IGAAjeton", "peroche", obtenuLe, obtenuLe + AccesInstagram.DureeDuJeton);

        Assert.True(copie.DoitEtreRenouvele(Banc.Instant));
        Assert.False(copie.EstExpire(Banc.Instant));
        Assert.True(copie.EstExpire(obtenuLe + AccesInstagram.DureeDuJeton));
        Assert.False(copie.DoitEtreRenouvele(obtenuLe.AddDays(6)));
    }

    [Fact]
    public void La_copie_ne_montre_jamais_le_jeton_dans_un_journal()
    {
        var copie = new AccesInstagramPublie(new CopieAccesInstagram("IGAAsecret", "peroche", Banc.Instant, Banc.Instant.AddDays(60)));

        Assert.DoesNotContain("IGAAsecret", copie.ToString(), StringComparison.Ordinal);
    }
}
