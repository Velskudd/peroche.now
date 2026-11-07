using PerocheNow.Domain.Commun;

namespace PerocheNow.Domain.Medias;

/// <summary>Ce qu'un emplacement du site accepte : une photo ou une vidéo.</summary>
public enum GenreDeMedia
{
    Image,
    Video,
}

/// <summary>
/// Une photo ou une vidéo envoyée depuis l'administration pour un emplacement du site
/// (l'ouverture de l'accueil, la bulle d'une étape…). Le fichier d'origine reste dans le code :
/// retirer le média le fait revenir.
/// </summary>
public sealed class MediaDuSite : Entite
{
    public const int LongueurMaxCle = 100;
    public const int LongueurMaxFichier = 200;

    /// <summary>L'emplacement, par exemple « accueil.ouverture.video ».</summary>
    public string Cle { get; private set; } = string.Empty;

    /// <summary>Le nom du fichier dans le stockage ; il change à chaque envoi, ce qui permet un cache très long.</summary>
    public string Fichier { get; private set; } = string.Empty;

    public string TypeContenu { get; private set; } = string.Empty;
    public long Taille { get; private set; }
    public DateTimeOffset ModifieLe { get; private set; }

    // Constructeur réservé à l'ORM.
    private MediaDuSite() { }

    public static MediaDuSite Creer(string cle, FichierMedia fichier, DateTimeOffset maintenant)
    {
        if (string.IsNullOrWhiteSpace(cle) || cle.Length > LongueurMaxCle)
            throw new RegleMetierException("L'emplacement du média est invalide.");

        var media = new MediaDuSite { Cle = cle };
        media.Remplacer(fichier, maintenant);
        return media;
    }

    public void Remplacer(FichierMedia fichier, DateTimeOffset maintenant)
    {
        Fichier = fichier.Nom;
        TypeContenu = fichier.TypeContenu;
        Taille = fichier.Taille;
        ModifieLe = maintenant;
    }
}

/// <summary>Un fichier vérifié, prêt à être rangé : son nom dans le stockage, son type et sa taille.</summary>
public sealed record FichierMedia(string Nom, string TypeContenu, long Taille);
