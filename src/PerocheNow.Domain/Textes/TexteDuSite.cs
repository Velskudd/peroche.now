using PerocheNow.Domain.Commun;

namespace PerocheNow.Domain.Textes;

/// <summary>
/// Un texte du site réécrit depuis l'administration. Le texte d'origine reste dans le code :
/// seul ce qui a été modifié est enregistré, et supprimer la modification rend le texte d'origine.
/// </summary>
public sealed class TexteDuSite : Entite
{
    public const int LongueurMaxCle = 100;
    public const int LongueurMaxValeur = 2000;

    /// <summary>Repère du texte dans le site, par exemple « accueil.ouverture.bouton-projet ».</summary>
    public string Cle { get; private set; } = string.Empty;
    public string Valeur { get; private set; } = string.Empty;
    public DateTimeOffset ModifieLe { get; private set; }

    // Constructeur réservé à l'ORM.
    private TexteDuSite() { }

    public static TexteDuSite Creer(string cle, string valeur, DateTimeOffset maintenant)
    {
        if (string.IsNullOrWhiteSpace(cle))
            throw new RegleMetierException("Un texte doit avoir un repère.");

        var nettoyee = cle.Trim();
        if (nettoyee.Length > LongueurMaxCle)
            throw new RegleMetierException($"Le repère d'un texte ne doit pas dépasser {LongueurMaxCle} caractères.");

        var texte = new TexteDuSite { Cle = nettoyee };
        texte.Modifier(valeur, maintenant);
        return texte;
    }

    public void Modifier(string valeur, DateTimeOffset maintenant)
    {
        if (string.IsNullOrWhiteSpace(valeur))
            throw new RegleMetierException("Un texte modifié ne peut pas être vide : vide le champ pour revenir au texte d'origine.");

        var nettoyee = valeur.Trim();
        if (nettoyee.Length > LongueurMaxValeur)
            throw new RegleMetierException($"Un texte ne doit pas dépasser {LongueurMaxValeur} caractères.");

        Valeur = nettoyee;
        ModifieLe = maintenant;
    }
}
