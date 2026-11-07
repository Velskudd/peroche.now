using PerocheNow.Domain.Medias;
using PerocheNow.Web.Textes;

namespace PerocheNow.Web.Medias;

/// <summary>
/// Un emplacement de photo ou de vidéo du site : où il se trouve, ce qu'il accepte, son fichier d'origine
/// (dans wwwroot, ou aucun) et, pour une photo qui porte du sens, sa description pour les lecteurs d'écran.
/// </summary>
public sealed record EmplacementMedia(
    string Cle,
    string Page,
    string Libelle,
    GenreDeMedia Genre,
    string? ParDefaut,
    string Conseil,
    DefinitionTexte? Description);

/// <summary>Tous les emplacements de photos et de vidéos modifiables depuis l'administration.</summary>
public static class CatalogueDesMedias
{
    private const string Portrait = "JPEG, PNG ou WebP, en portrait (3:4 ou 4:5), 1000 px de large environ, moins de 8 Mo.";
    private const string Bulle = "JPEG, PNG ou WebP, en paysage (3:2), 800 px de large environ, moins de 8 Mo.";

    public static readonly EmplacementMedia OuvertureVideo = new(
        "accueil.ouverture.video", "Accueil", "Vidéo de l'ouverture", GenreDeMedia.Video, "videos/accueil.mp4",
        "MP4 (H.264), en paysage (16:9), 8 à 20 secondes, sans son, moins de 40 Mo (idéalement moins de 5 Mo). Elle passe en noir et blanc sous le voile du site.",
        null);

    public static readonly EmplacementMedia OuvertureImage = new(
        "accueil.ouverture.image", "Accueil", "Image fixe de l'ouverture", GenreDeMedia.Image, "images/accueil-poster.jpg",
        "La première image de la vidéo, de préférence : elle s'affiche le temps que la vidéo charge, et à sa place pour les visiteurs qui économisent leurs données. JPEG ou WebP, 1280 × 720 px.",
        null);

    public static readonly EmplacementMedia Partage = new(
        "partage.image", "Accueil", "Aperçu d'un lien partagé (réseaux, messageries)", GenreDeMedia.Image, "images/combinaison-1040.jpg",
        "JPEG ou PNG, 1200 × 630 px de préférence (les réseaux recadrent en paysage).",
        null);

    public static readonly EmplacementMedia Nur24OuJenSuis = new(
        "nur24.ou.photo", "NUR24", "Photo de « Où j'en suis »", GenreDeMedia.Image, "images/showroom.webp", Portrait,
        Description("nur24.ou.photo", "Alexandre en combinaison, mains jointes, dans un showroom"));

    public static readonly EmplacementMedia PartenairesPhoto = new(
        "partenaires.photo", "Partenaires", "Photo du haut de page", GenreDeMedia.Image, "images/casque-bras-croises.webp", Portrait,
        Description("partenaires.photo", "Alexandre casqué, en combinaison, bras croisés"));

    public static readonly EmplacementMedia LiensPhoto = new(
        "liens.photo", "Page de liens", "Photo du haut de page", GenreDeMedia.Image, "images/volant-regard.webp",
        "JPEG, PNG ou WebP ; elle est recadrée en bandeau (le haut du visage reste visible). Moins de 8 Mo.",
        Description("liens.photo", "Alexandre au volant, en combinaison de course"));

    /// <summary>Une photo par saison de la route, dans la bulle de son secteur.</summary>
    public static readonly IReadOnlyList<EmplacementMedia> Route =
    [
        BulleDeRoute(2026, "images/volant-regard.webp", "Alexandre au volant, en combinaison de course"),
        BulleDeRoute(2027),
        BulleDeRoute(2028),
        BulleDeRoute(2029),
    ];

    /// <summary>Une photo par étape de « D'où je pars », dans la bulle de son secteur (huit au plus).</summary>
    public static readonly IReadOnlyList<EmplacementMedia> Parcours =
        Enumerable.Range(1, 8).Select(BulleDeParcours).ToList();

    public static IReadOnlyList<EmplacementMedia> Tous { get; } =
    [
        OuvertureVideo, OuvertureImage, Partage,
        Nur24OuJenSuis, .. Route, .. Parcours,
        PartenairesPhoto, LiensPhoto,
    ];

    public static EmplacementMedia? Trouver(string? cle) =>
        Tous.FirstOrDefault(e => string.Equals(e.Cle, cle, StringComparison.Ordinal));

    private static EmplacementMedia BulleDeRoute(int annee) => BulleDeRoute(annee, null, string.Empty);

    private static EmplacementMedia BulleDeRoute(int annee, string? parDefaut, string description) => new(
        $"nur24.route.{annee}.photo", "Circuits : la route", $"Bulle {annee}", GenreDeMedia.Image, parDefaut, Bulle,
        Description($"nur24.route.{annee}.photo", description));

    private static EmplacementMedia BulleDeParcours(int numero) => new(
        $"nur24.parcours.{numero}.photo", "Circuits : d'où je pars", $"Bulle de l'étape {numero}", GenreDeMedia.Image, null, Bulle,
        Description($"nur24.parcours.{numero}.photo", string.Empty));

    /// <summary>La description d'une photo est un texte du site comme un autre, modifié depuis l'onglet Médias.</summary>
    private static DefinitionTexte Description(string cle, string parDefaut) =>
        new($"media.{cle}.description", "Médias", "Description de la photo", parDefaut, 200, Saisie.Ligne);
}
