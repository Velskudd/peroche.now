using PerocheNow.Business.Contenus.Instagram;
using PerocheNow.Domain.Commun;
using PerocheNow.Domain.Plateformes;

namespace PerocheNow.Business.Tests.Contenus;

public sealed class AccesInstagramCasDUsageTests
{
    [Fact]
    public async Task Un_jeton_valide_branche_le_compte_pour_soixante_jours()
    {
        using var banc = new Banc();
        using var portee = banc.NouvellePortee();

        var id = await Banc.Mediateur(portee).EnvoyerAsync(new ConnecterInstagram($"  {FausseApiInstagram.JetonValide} "));

        var acces = Assert.IsType<AccesInstagram>(banc.AccesInstagram.Acces);
        Assert.Equal(id, acces.Id);
        Assert.Equal(FausseApiInstagram.JetonValide, acces.Jeton);
        Assert.Equal("peroche", acces.Compte);
        Assert.Equal(Banc.Instant + AccesInstagram.DureeDuJeton, acces.ExpireLe);
        Assert.Equal(1, banc.UniteDeTravail.Enregistrements);
        Assert.Equal(nameof(ConnecterInstagram), Assert.Single(banc.Traces.Traces).Action);
    }

    [Fact]
    public async Task Un_jeton_refuse_par_Instagram_n_est_pas_enregistre()
    {
        using var banc = new Banc();
        using var portee = banc.NouvellePortee();

        await Assert.ThrowsAsync<RegleMetierException>(() =>
            Banc.Mediateur(portee).EnvoyerAsync(new ConnecterInstagram("IGAAfaux")));

        Assert.Null(banc.AccesInstagram.Acces);
        Assert.Equal(0, banc.UniteDeTravail.Enregistrements);
    }

    [Fact]
    public async Task Le_renouvellement_ne_fait_rien_avant_une_semaine()
    {
        using var banc = new Banc();
        banc.AccesInstagram.Acces = AccesInstagram.Creer("IGAAancien", "peroche", Banc.Instant.AddDays(55), Banc.Instant.AddDays(-5));
        using var portee = banc.NouvellePortee();

        var renouvele = await Banc.Mediateur(portee).EnvoyerAsync(new RenouvelerAccesInstagram());

        Assert.Null(renouvele);
        Assert.Equal(0, banc.Instagram.Renouvellements);
        Assert.Equal("IGAAancien", banc.AccesInstagram.Acces.Jeton);
    }

    [Fact]
    public async Task Apres_une_semaine_le_jeton_est_prolonge_et_l_etat_le_montre()
    {
        using var banc = new Banc();
        banc.AccesInstagram.Acces = AccesInstagram.Creer("IGAAancien", "peroche", Banc.Instant.AddDays(50), Banc.Instant.AddDays(-10));
        using var portee = banc.NouvellePortee();
        var mediateur = Banc.Mediateur(portee);

        var avant = await mediateur.EnvoyerAsync(new ObtenirEtatInstagram());
        var renouvele = await mediateur.EnvoyerAsync(new RenouvelerAccesInstagram());
        var apres = await mediateur.EnvoyerAsync(new ObtenirEtatInstagram());

        Assert.True(avant is { DoitEtreRenouvele: true });
        Assert.Equal(banc.AccesInstagram.Acces.Id, renouvele);
        Assert.Equal("IGAArenouvele1", banc.AccesInstagram.Acces.Jeton);
        Assert.True(apres is { DoitEtreRenouvele: false });
        Assert.Equal(Banc.Instant.AddDays(60), apres?.ExpireLe);
    }
}
