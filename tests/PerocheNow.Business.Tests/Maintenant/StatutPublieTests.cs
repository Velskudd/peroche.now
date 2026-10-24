using PerocheNow.Business.Audit;
using PerocheNow.Business.CopiesDeLecture;
using PerocheNow.Business.Maintenant;
using PerocheNow.Domain.Commun;
using PerocheNow.Domain.Maintenant;

namespace PerocheNow.Business.Tests.Maintenant;

/// <summary>La copie de lecture du statut : publiée après la base, lue à sa place par les pages publiques.</summary>
public sealed class StatutPublieTests
{
    private static DefinirStatutDuMoment Commande(string accroche = "Je roule sur la Nordschleife.") =>
        new(accroche, [new LigneSaisie("Sur iRacing, en NEC.", Rubrique.ProjetEnCours)], "RCN 2027", new DateOnly(2027, 9, 1));

    [Fact]
    public async Task Enregistrer_le_statut_publie_sa_copie_apres_la_base()
    {
        using var banc = new Banc();
        using var portee = banc.NouvellePortee();

        await Banc.Mediateur(portee).EnvoyerAsync(Commande());

        var (nom, enregistrementsAvant) = Assert.Single(banc.Copies.Publications);
        Assert.Equal(NomsDesCopies.StatutDuMoment, nom);
        Assert.Equal(1, enregistrementsAvant);

        var copie = Assert.IsType<StatutDuMomentDto>(banc.Copies.Copie<StatutDuMomentDto>(NomsDesCopies.StatutDuMoment));
        Assert.Equal("Je roule sur la Nordschleife.", copie.Accroche);
        Assert.Equal(Rubrique.ProjetEnCours, Assert.Single(copie.Lignes).Rubrique);
        Assert.Equal(new DateOnly(2027, 9, 1), copie.ProchainObjectif?.DateCible);
    }

    [Fact]
    public async Task Une_commande_refusee_ne_publie_rien()
    {
        using var banc = new Banc();
        using var portee = banc.NouvellePortee();

        await Assert.ThrowsAsync<RegleMetierException>(() => Banc.Mediateur(portee).EnvoyerAsync(Commande(accroche: "")));

        Assert.Empty(banc.Copies.Publications);
        Assert.Equal(0, banc.UniteDeTravail.Enregistrements);
    }

    [Fact]
    public async Task Une_copie_impossible_a_publier_est_signalee_mais_la_base_est_gardee()
    {
        using var banc = new Banc();
        banc.Copies.EnPanne = true;
        using var portee = banc.NouvellePortee();

        await Assert.ThrowsAsync<PublicationIncompleteException>(() => Banc.Mediateur(portee).EnvoyerAsync(Commande()));

        Assert.Equal(1, banc.UniteDeTravail.Enregistrements);
        Assert.NotNull(banc.Statuts.Statut);
    }

    [Fact]
    public async Task L_accueil_lit_la_copie_sans_toucher_la_base()
    {
        using var banc = new Banc();
        var publie = new StatutDuMomentDto("Depuis la copie.", [], null, Banc.Instant);
        banc.Copies.Deposer(NomsDesCopies.StatutDuMoment, publie);
        using var portee = banc.NouvellePortee();

        var statut = await Banc.Mediateur(portee).EnvoyerAsync(new ObtenirStatutPublie());

        Assert.Same(publie, statut);
        Assert.Equal(0, banc.Statuts.Lectures);
    }

    [Fact]
    public async Task Sans_copie_l_accueil_la_reconstruit_une_seule_fois_depuis_la_base()
    {
        using var banc = new Banc();
        banc.Statuts.Statut = StatutDuMoment.Creer("Depuis la base.", Banc.Instant);

        using (var premiere = banc.NouvellePortee())
            Assert.Equal("Depuis la base.", (await Banc.Mediateur(premiere).EnvoyerAsync(new ObtenirStatutPublie()))?.Accroche);

        using (var seconde = banc.NouvellePortee())
            Assert.Equal("Depuis la base.", (await Banc.Mediateur(seconde).EnvoyerAsync(new ObtenirStatutPublie()))?.Accroche);

        Assert.Equal(1, banc.Statuts.Lectures);
        Assert.Equal(NomsDesCopies.StatutDuMoment, Assert.Single(banc.Copies.Publications).Nom);
    }

    [Fact]
    public async Task L_administration_lit_toujours_la_base()
    {
        using var banc = new Banc();
        banc.Statuts.Statut = StatutDuMoment.Creer("Depuis la base.", Banc.Instant);
        banc.Copies.Deposer(NomsDesCopies.StatutDuMoment, new StatutDuMomentDto("Copie périmée.", [], null, Banc.Instant));
        using var portee = banc.NouvellePortee();

        var statut = await Banc.Mediateur(portee).EnvoyerAsync(new ObtenirStatutDuMoment());

        Assert.Equal("Depuis la base.", statut?.Accroche);
    }
}
