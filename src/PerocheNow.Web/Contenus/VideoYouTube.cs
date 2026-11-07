using Microsoft.AspNetCore.WebUtilities;

namespace PerocheNow.Web.Contenus;

/// <summary>
/// Une vidéo YouTube désignée dans l'administration, par son adresse (youtu.be, youtube.com/watch, shorts, live)
/// ou son identifiant seul. Tout autre lien est ignoré : seule une vidéo YouTube peut s'afficher ainsi.
/// </summary>
public sealed record VideoYouTube(Uri Lien, Uri Miniature, Uri MiniatureHauteDefinition)
{
    private const int LongueurIdentifiant = 11;
    private const string HoteCourt = "youtu.be";
    private const string HoteMiniatures = "i.ytimg.com";

    private static readonly string[] HotesYouTube = ["youtube.com", "www.youtube.com", "m.youtube.com"];
    private static readonly string[] Rubriques = ["shorts", "live", "embed"];

    /// <summary>La vidéo, ou <c>null</c> si la saisie est vide ou ne désigne pas une vidéo YouTube.</summary>
    public static VideoYouTube? Lire(string? saisie)
    {
        var texte = saisie?.Trim();
        if (string.IsNullOrEmpty(texte))
            return null;
        if (EstIdentifiant(texte))
            return Depuis(texte);
        if (!Uri.TryCreate(texte, UriKind.Absolute, out var adresse) || adresse.Scheme != Uri.UriSchemeHttps)
            return null;

        var identifiant = Identifiant(adresse);
        return identifiant is not null && EstIdentifiant(identifiant) ? Depuis(identifiant) : null;
    }

    private static string? Identifiant(Uri adresse)
    {
        var hote = adresse.Host.ToLowerInvariant();
        var segments = adresse.AbsolutePath.Trim('/').Split('/');

        if (hote == HoteCourt)
            return segments[0];
        if (!HotesYouTube.Contains(hote))
            return null;
        if (segments[0] == "watch")
            return QueryHelpers.ParseQuery(adresse.Query).TryGetValue("v", out var v) ? v.ToString() : null;
        return segments.Length == 2 && Rubriques.Contains(segments[0]) ? segments[1] : null;
    }

    private static bool EstIdentifiant(string texte) =>
        texte.Length == LongueurIdentifiant && texte.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    private static VideoYouTube Depuis(string identifiant) => new(
        new UriBuilder(Uri.UriSchemeHttps, HoteCourt, -1, identifiant).Uri,
        new UriBuilder(Uri.UriSchemeHttps, HoteMiniatures, -1, $"vi/{identifiant}/mqdefault.jpg").Uri,
        new UriBuilder(Uri.UriSchemeHttps, HoteMiniatures, -1, $"vi/{identifiant}/maxresdefault.jpg").Uri);
}
