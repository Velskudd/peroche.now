using PerocheNow.Domain.Commun;
using PerocheNow.Domain.Maintenant;

namespace PerocheNow.Domain.Tests.Maintenant;

public sealed class StatutDuMomentTests
{
    private static readonly DateTimeOffset Lundi = new(2026, 10, 12, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Mardi = Lundi.AddDays(1);

    [Fact]
    public void Creer_nettoie_l_accroche_et_date_la_mise_a_jour()
    {
        var statut = StatutDuMoment.Creer("  Je prépare ma première semaine sur la Nordschleife.  ", Lundi);

        Assert.Equal("Je prépare ma première semaine sur la Nordschleife.", statut.Accroche);
        Assert.Equal(Lundi, statut.MisAJourLe);
        Assert.Empty(statut.Lignes);
        Assert.Null(statut.ProchainObjectif);
        Assert.NotEqual(Guid.Empty, statut.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Creer_refuse_une_accroche_vide(string accroche)
    {
        Assert.Throws<RegleMetierException>(() => StatutDuMoment.Creer(accroche, Lundi));
    }

    [Fact]
    public void Creer_refuse_une_accroche_trop_longue()
    {
        var accroche = new string('a', StatutDuMoment.LongueurMaxAccroche + 1);

        Assert.Throws<RegleMetierException>(() => StatutDuMoment.Creer(accroche, Lundi));
    }

    [Fact]
    public void Une_accroche_a_la_longueur_maximale_est_acceptee()
    {
        var accroche = new string('a', StatutDuMoment.LongueurMaxAccroche);

        var statut = StatutDuMoment.Creer(accroche, Lundi);

        Assert.Equal(accroche, statut.Accroche);
    }

    [Fact]
    public void ModifierAccroche_remplace_le_texte_et_la_date()
    {
        var statut = StatutDuMoment.Creer("Avant", Lundi);

        statut.ModifierAccroche("Après", Mardi);

        Assert.Equal("Après", statut.Accroche);
        Assert.Equal(Mardi, statut.MisAJourLe);
    }

    [Fact]
    public void RemplacerLignes_garde_l_ordre_donne_et_ignore_les_lignes_vides()
    {
        var statut = StatutDuMoment.Creer("Accroche", Lundi);

        statut.RemplacerLignes(["  Première  ", "", "Deuxième", "   "], Mardi);

        var lignes = statut.Lignes.OrderBy(l => l.Ordre).ToList();
        Assert.Collection(
            lignes,
            l => { Assert.Equal(1, l.Ordre); Assert.Equal("Première", l.Texte); },
            l => { Assert.Equal(2, l.Ordre); Assert.Equal("Deuxième", l.Texte); });
        Assert.Equal(Mardi, statut.MisAJourLe);
    }

    [Fact]
    public void RemplacerLignes_remplace_les_lignes_precedentes()
    {
        var statut = StatutDuMoment.Creer("Accroche", Lundi);
        statut.RemplacerLignes(["Une", "Deux", "Trois"], Lundi);

        statut.RemplacerLignes(["Seule"], Mardi);

        var ligne = Assert.Single(statut.Lignes);
        Assert.Equal("Seule", ligne.Texte);
        Assert.Equal(1, ligne.Ordre);
    }

    [Fact]
    public void RemplacerLignes_refuse_plus_de_trois_lignes()
    {
        var statut = StatutDuMoment.Creer("Accroche", Lundi);

        Assert.Throws<RegleMetierException>(() => statut.RemplacerLignes(["1", "2", "3", "4"], Mardi));
    }

    [Fact]
    public void RemplacerLignes_refuse_une_ligne_trop_longue_sans_rien_modifier()
    {
        var statut = StatutDuMoment.Creer("Accroche", Lundi);
        statut.RemplacerLignes(["Existante"], Lundi);

        Assert.Throws<RegleMetierException>(() =>
            statut.RemplacerLignes([new string('a', StatutDuMoment.LongueurMaxLigne + 1)], Mardi));

        Assert.Equal("Existante", Assert.Single(statut.Lignes).Texte);
        Assert.Equal(Lundi, statut.MisAJourLe);
    }

    [Fact]
    public void DefinirProchainObjectif_peut_poser_puis_retirer_l_objectif()
    {
        var statut = StatutDuMoment.Creer("Accroche", Lundi);

        statut.DefinirProchainObjectif(new Objectif("Test Week Nordschleife"), Mardi);
        Assert.Equal("Test Week Nordschleife", statut.ProchainObjectif?.Libelle);

        statut.DefinirProchainObjectif(null, Mardi);
        Assert.Null(statut.ProchainObjectif);
    }
}
