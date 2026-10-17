using PerocheNow.Business.Maintenant;
using PerocheNow.Domain.Commun;
using PerocheNow.Domain.Maintenant;

namespace PerocheNow.Business.Tests.Maintenant;

public sealed class StatutDuMomentCasDUsageTests
{
    private static DefinirStatutDuMoment Commande(params string[] lignes) =>
        new("Je prépare ma première semaine sur la Nordschleife.", lignes, "Test Week Nordschleife", null);

    [Fact]
    public async Task Definir_cree_le_statut_quand_il_n_existe_pas()
    {
        using var banc = new Banc();
        using var portee = banc.NouvellePortee();

        var id = await banc.Mediateur(portee).EnvoyerAsync(Commande("Une", "Deux"));

        var statut = Assert.IsType<StatutDuMoment>(banc.Statuts.Statut);
        Assert.Equal(id, statut.Id);
        Assert.Equal(2, statut.Lignes.Count);
        Assert.Equal("Test Week Nordschleife", statut.ProchainObjectif?.Libelle);
        Assert.Equal(Banc.Instant, statut.MisAJourLe);
    }

    [Fact]
    public async Task Definir_enregistre_une_seule_fois_avec_sa_trace_d_audit()
    {
        using var banc = new Banc();
        using var portee = banc.NouvellePortee();

        var id = await banc.Mediateur(portee).EnvoyerAsync(Commande("Une"));

        Assert.Equal(1, banc.UniteDeTravail.Enregistrements);
        var trace = Assert.Single(banc.Traces.Traces);
        Assert.Equal(nameof(DefinirStatutDuMoment), trace.Action);
        Assert.Equal("alexandre", trace.Auteur);
        Assert.Equal(id, trace.IdObjet);
        Assert.Equal(Banc.Instant, trace.Le);
    }

    [Fact]
    public async Task Definir_met_a_jour_le_statut_existant_sans_en_creer_un_autre()
    {
        using var banc = new Banc();
        var existant = StatutDuMoment.Creer("Avant", Banc.Instant.AddDays(-3));
        banc.Statuts.Statut = existant;
        using var portee = banc.NouvellePortee();

        var id = await banc.Mediateur(portee).EnvoyerAsync(
            new DefinirStatutDuMoment("Après", ["Seule ligne"], Objectif: null, DateObjectif: null));

        Assert.Equal(existant.Id, id);
        Assert.Same(existant, banc.Statuts.Statut);
        Assert.Equal("Après", existant.Accroche);
        Assert.Null(existant.ProchainObjectif);
        Assert.Equal(Banc.Instant, existant.MisAJourLe);
    }

    [Fact]
    public async Task Une_regle_metier_violee_n_enregistre_rien_et_ne_trace_rien()
    {
        using var banc = new Banc();
        using var portee = banc.NouvellePortee();

        await Assert.ThrowsAsync<RegleMetierException>(() =>
            banc.Mediateur(portee).EnvoyerAsync(Commande("1", "2", "3", "4")));

        Assert.Equal(0, banc.UniteDeTravail.Enregistrements);
        Assert.Empty(banc.Traces.Traces);
    }

    [Fact]
    public async Task Obtenir_renvoie_null_tant_que_rien_n_est_defini()
    {
        using var banc = new Banc();
        using var portee = banc.NouvellePortee();

        var statut = await banc.Mediateur(portee).EnvoyerAsync(new ObtenirStatutDuMoment());

        Assert.Null(statut);
    }

    [Fact]
    public async Task Obtenir_renvoie_les_lignes_dans_l_ordre_sans_tracer_ni_enregistrer()
    {
        using var banc = new Banc();
        var statut = StatutDuMoment.Creer("Accroche", Banc.Instant);
        statut.RemplacerLignes(["Première", "Deuxième", "Troisième"], Banc.Instant);
        statut.DefinirProchainObjectif(new Objectif("Test Week Nordschleife", new DateOnly(2027, 3, 15)), Banc.Instant);
        banc.Statuts.Statut = statut;
        using var portee = banc.NouvellePortee();

        var dto = await banc.Mediateur(portee).EnvoyerAsync(new ObtenirStatutDuMoment());

        Assert.NotNull(dto);
        Assert.Equal("Accroche", dto.Accroche);
        string[] lignesAttendues = ["Première", "Deuxième", "Troisième"];
        Assert.Equal(lignesAttendues, dto.Lignes);
        Assert.Equal(new ObjectifDto("Test Week Nordschleife", new DateOnly(2027, 3, 15)), dto.ProchainObjectif);
        Assert.Equal(0, banc.UniteDeTravail.Enregistrements);
        Assert.Empty(banc.Traces.Traces);
    }

    [Fact]
    public void Le_statut_de_secours_reprend_le_statut_initial()
    {
        var secours = StatutInitial.CommeStatutDeSecours(Banc.Instant);

        Assert.Equal(StatutInitial.Commande.Accroche, secours.Accroche);
        Assert.Equal(StatutInitial.Commande.Lignes, secours.Lignes);
        Assert.Equal(StatutInitial.Commande.Objectif, secours.ProchainObjectif?.Libelle);
    }

    [Fact]
    public async Task Le_statut_initial_respecte_les_regles_du_domaine()
    {
        using var banc = new Banc();
        using var portee = banc.NouvellePortee();

        await banc.Mediateur(portee).EnvoyerAsync(StatutInitial.Commande);

        Assert.NotNull(banc.Statuts.Statut);
    }
}
