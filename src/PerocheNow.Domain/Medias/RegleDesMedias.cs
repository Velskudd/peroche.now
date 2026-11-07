using System.Security.Cryptography;
using System.Text;
using PerocheNow.Domain.Commun;

namespace PerocheNow.Domain.Medias;

/// <summary>
/// Ce que le site accepte comme photo ou vidéo. Le type est reconnu à la signature du fichier
/// (ses premiers octets), jamais à son nom ni à ce que déclare le navigateur.
/// </summary>
public static class RegleDesMedias
{
    public const long TailleMaxImage = 8 * 1024 * 1024;
    public const long TailleMaxVideo = 40 * 1024 * 1024;

    private sealed record Format(string TypeContenu, string Extension, GenreDeMedia Genre);

    private static readonly Format Jpeg = new("image/jpeg", "jpg", GenreDeMedia.Image);
    private static readonly Format Png = new("image/png", "png", GenreDeMedia.Image);
    private static readonly Format WebP = new("image/webp", "webp", GenreDeMedia.Image);
    private static readonly Format Mp4 = new("video/mp4", "mp4", GenreDeMedia.Video);

    /// <summary>
    /// Vérifie le fichier pour un emplacement et lui donne son nom de stockage :
    /// l'emplacement, puis une empreinte du contenu (un nouveau fichier a toujours un nouveau nom).
    /// </summary>
    public static FichierMedia Verifier(string cle, GenreDeMedia genre, ReadOnlySpan<byte> contenu)
    {
        if (contenu.IsEmpty)
            throw new RegleMetierException("Le fichier est vide.");

        var format = Reconnaitre(contenu);
        if (format is null || format.Genre != genre)
        {
            throw new RegleMetierException(genre == GenreDeMedia.Image
                ? "Cet emplacement attend une photo en JPEG, PNG ou WebP."
                : "Cet emplacement attend une vidéo MP4 (H.264).");
        }

        var tailleMax = genre == GenreDeMedia.Image ? TailleMaxImage : TailleMaxVideo;
        if (contenu.Length > tailleMax)
            throw new RegleMetierException($"Le fichier dépasse {tailleMax / (1024 * 1024)} Mo : compresse-le avant de l'envoyer.");

        var empreinte = Convert.ToHexStringLower(SHA256.HashData(contenu))[..12];
        return new FichierMedia($"{NomSur(cle)}-{empreinte}.{format.Extension}", format.TypeContenu, contenu.Length);
    }

    /// <summary>Vrai si le nom a la forme d'un fichier rangé par le site (sert à refuser tout autre chemin).</summary>
    public static bool EstUnNomDeFichier(string nom) =>
        nom.Length is > 0 and <= MediaDuSite.LongueurMaxFichier
        && nom.All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' or '.')
        && !nom.Contains("..", StringComparison.Ordinal);

    private static Format? Reconnaitre(ReadOnlySpan<byte> contenu)
    {
        if (contenu.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]))
            return Jpeg;
        if (contenu.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
            return Png;
        if (contenu.Length >= 12 && contenu[..4].SequenceEqual("RIFF"u8) && contenu[8..12].SequenceEqual("WEBP"u8))
            return WebP;
        if (contenu.Length >= 12 && contenu[4..8].SequenceEqual("ftyp"u8))
            return Mp4;
        return null;
    }

    /// <summary>L'emplacement réduit aux lettres, chiffres, points et tirets.</summary>
    private static string NomSur(string cle)
    {
        var nom = new StringBuilder(cle.Length);
        foreach (var c in cle.ToLowerInvariant())
            nom.Append(c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '.' ? c : '-');
        return nom.ToString().Trim('-', '.');
    }
}
