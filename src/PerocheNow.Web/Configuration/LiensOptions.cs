namespace PerocheNow.Web.Configuration;

/// <summary>
/// Les liens sortants du site, section <c>Liens</c> de la configuration.
/// Un lien vide n'est pas affiché : on ajoute un réseau en renseignant simplement son adresse.
/// </summary>
public sealed class LiensOptions
{
    public const string Section = "Liens";

    public string? Twitch { get; init; }
    public string? YouTube { get; init; }
    public string? Instagram { get; init; }
    public string? Facebook { get; init; }
    public string? TikTok { get; init; }

    /// <summary>Le dossier partenaire (PDF ou page). Tant qu'il est vide, le bouton n'apparaît pas.</summary>
    public string? DossierPartenaire { get; init; }

    /// <summary>L'adresse e-mail de contact pour les partenariats (Trinity Management). Vide : pas de lien mailto.</summary>
    public string? ContactPartenariats { get; init; }

    /// <summary>Les réseaux renseignés, dans l'ordre d'affichage du pied de page.</summary>
    public IEnumerable<(string Nom, string Adresse)> Reseaux()
    {
        (string Nom, string? Adresse)[] tous =
        [
            ("Twitch", Twitch),
            ("YouTube", YouTube),
            ("Instagram", Instagram),
            ("Facebook", Facebook),
            ("TikTok", TikTok),
        ];

        return tous
            .Where(r => !string.IsNullOrWhiteSpace(r.Adresse))
            .Select(r => (r.Nom, r.Adresse!));
    }
}
