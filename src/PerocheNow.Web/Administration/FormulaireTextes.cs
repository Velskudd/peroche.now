using PerocheNow.Business.Textes;
using PerocheNow.Web.Textes;

namespace PerocheNow.Web.Administration;

/// <summary>
/// Les textes d'une page du site, tels qu'affichés dans l'administration : chaque champ part du texte en place.
/// À l'envoi, un champ vide ou égal au texte d'origine rétablit celui-ci ; le reste devient un texte modifié.
/// </summary>
public sealed class FormulaireTextes
{
    public List<ChampTexte> Champs { get; set; } = [];

    public static FormulaireTextes Depuis(PageDeTextes page, IReadOnlyDictionary<string, string> modifies) => new()
    {
        Champs = page.Textes
            .Select(texte => new ChampTexte
            {
                Cle = texte.Cle,
                Valeur = modifies.TryGetValue(texte.Cle, out var modifie) ? modifie : texte.ParDefaut,
            })
            .ToList(),
    };

    /// <summary>Les textes trop longs pour leur emplacement, avec leur limite.</summary>
    public IReadOnlyList<string> Erreurs(PageDeTextes page) =>
        Associer(page)
            .Where(paire => Nettoyer(paire.Champ.Valeur).Length > paire.Texte.LongueurMax)
            .Select(paire => $"« {paire.Texte.Libelle} » ({paire.Texte.Section}) ne doit pas dépasser {paire.Texte.LongueurMax} caractères.")
            .ToList();

    /// <summary>Ce qu'il faut enregistrer : un texte modifié, ou rien pour revenir au texte d'origine.</summary>
    public ModifierTextesDuSite EnCommande(PageDeTextes page) => new(
        Associer(page)
            .Select(paire =>
            {
                var valeur = Nettoyer(paire.Champ.Valeur);
                var origine = valeur.Length == 0 || string.Equals(valeur, Nettoyer(paire.Texte.ParDefaut), StringComparison.Ordinal);
                return new TexteSaisi(paire.Texte.Cle, origine ? null : valeur);
            })
            .ToList());

    /// <summary>Chaque champ reçu avec sa définition ; un repère inconnu de la page est ignoré.</summary>
    private IEnumerable<(DefinitionTexte Texte, ChampTexte Champ)> Associer(PageDeTextes page) =>
        Champs.Join(page.Textes, champ => champ.Cle, texte => texte.Cle, (champ, texte) => (texte, champ), StringComparer.Ordinal);

    /// <summary>Fins de ligne unifiées et espaces de bord retirés, ligne par ligne pour les listes.</summary>
    private static string Nettoyer(string? valeur) =>
        string.Join('\n', (valeur ?? string.Empty).ReplaceLineEndings("\n").Split('\n').Select(ligne => ligne.Trim())).Trim();
}

public sealed class ChampTexte
{
    public string Cle { get; set; } = string.Empty;
    public string? Valeur { get; set; }
}
