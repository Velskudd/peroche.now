using PerocheNow.Domain.Commun;

namespace PerocheNow.Domain.Maintenant;

/// <summary>
/// Ce qu'Alexandre fait en ce moment : la phrase d'accroche de l'accueil,
/// jusqu'à trois lignes complémentaires et le prochain objectif.
/// </summary>
public sealed class StatutDuMoment : Entite
{
    public const int LongueurMaxAccroche = 140;
    public const int NombreMaxLignes = 3;
    public const int LongueurMaxLigne = 200;

    private readonly List<LigneDuMoment> _lignes = [];

    public string Accroche { get; private set; } = string.Empty;
    /// <summary>Les lignes complémentaires. Utiliser <see cref="LigneDuMoment.Ordre"/> pour l'affichage.</summary>
    public IReadOnlyCollection<LigneDuMoment> Lignes => _lignes.AsReadOnly();
    public Objectif? ProchainObjectif { get; private set; }
    public DateTimeOffset MisAJourLe { get; private set; }

    // Constructeur réservé à l'ORM.
    private StatutDuMoment() { }

    public static StatutDuMoment Creer(string accroche, DateTimeOffset maintenant)
    {
        var statut = new StatutDuMoment();
        statut.ModifierAccroche(accroche, maintenant);
        return statut;
    }

    public void ModifierAccroche(string accroche, DateTimeOffset maintenant)
    {
        if (string.IsNullOrWhiteSpace(accroche))
            throw new RegleMetierException("L'accroche du moment est obligatoire.");

        var nettoyee = accroche.Trim();
        if (nettoyee.Length > LongueurMaxAccroche)
            throw new RegleMetierException($"L'accroche ne doit pas dépasser {LongueurMaxAccroche} caractères.");

        Accroche = nettoyee;
        MisAJourLe = maintenant;
    }

    /// <summary>
    /// Remplace toutes les lignes complémentaires. L'ordre de la liste devient l'ordre d'affichage.
    /// </summary>
    public void RemplacerLignes(IEnumerable<string> textes, DateTimeOffset maintenant)
    {
        var nettoyees = textes
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .ToList();

        if (nettoyees.Count > NombreMaxLignes)
            throw new RegleMetierException($"Pas plus de {NombreMaxLignes} lignes : l'accueil doit rester lisible d'un coup d'œil.");

        if (nettoyees.Any(t => t.Length > LongueurMaxLigne))
            throw new RegleMetierException($"Une ligne ne doit pas dépasser {LongueurMaxLigne} caractères.");

        _lignes.Clear();
        for (var i = 0; i < nettoyees.Count; i++)
            _lignes.Add(new LigneDuMoment(i + 1, nettoyees[i]));

        MisAJourLe = maintenant;
    }

    public void DefinirProchainObjectif(Objectif? objectif, DateTimeOffset maintenant)
    {
        ProchainObjectif = objectif;
        MisAJourLe = maintenant;
    }
}
