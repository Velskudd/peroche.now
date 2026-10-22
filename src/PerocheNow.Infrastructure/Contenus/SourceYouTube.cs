using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PerocheNow.Business.Contenus;
using PerocheNow.Business.Ports;

namespace PerocheNow.Infrastructure.Contenus;

/// <summary>
/// Adaptateur YouTube : lit un flux Atom public (aucune clé d'API) et le traduit en <see cref="Contenu"/>.
/// Un lien en <c>/shorts/</c> signale un format court, tout autre lien une vidéo.
/// </summary>
public sealed partial class SourceYouTube(IHttpClientFactory fabrique, Uri adresseFlux) : ISourceDeContenus
{
    public const string NomClient = "YouTube";

    public async Task<IReadOnlyList<Contenu>> ObtenirAsync(CancellationToken annulation)
    {
        await using var flux = await fabrique.CreateClient(NomClient).GetStreamAsync(adresseFlux, annulation);
        var document = await XDocument.LoadAsync(flux, LoadOptions.None, annulation);
        return Lire(document);
    }

    private static List<Contenu> Lire(XDocument document)
    {
        if (document.Root is not { } racine)
            return [];

        // Les espaces de noms sont lus dans le flux lui-même plutôt que recopiés ici.
        var atom = racine.Name.Namespace;
        var media = racine.GetNamespaceOfPrefix("media") ?? XNamespace.None;

        return racine.Elements(atom + "entry")
            .Select(entree => Convertir(entree, atom, media))
            .OfType<Contenu>()
            .ToList();
    }

    private static Contenu? Convertir(XElement entree, XNamespace atom, XNamespace media)
    {
        var lien = entree.Elements(atom + "link")
            .FirstOrDefault(l => (string?)l.Attribute("rel") == "alternate")?
            .Attribute("href")?.Value;
        var titre = (string?)entree.Element(atom + "title");
        var publication = (string?)entree.Element(atom + "published");

        if (!Uri.TryCreate(lien, UriKind.Absolute, out var adresse) || string.IsNullOrWhiteSpace(titre))
            return null;
        if (!DateTimeOffset.TryParse(publication, CultureInfo.InvariantCulture, DateTimeStyles.None, out var publieLe))
            return null;

        var miniature = entree.Element(media + "group")?.Element(media + "thumbnail")?.Attribute("url")?.Value;
        var format = adresse.AbsolutePath.StartsWith("/shorts/", StringComparison.Ordinal)
            ? FormatDeContenu.Court
            : FormatDeContenu.Video;

        return new Contenu(
            Plateforme.YouTube,
            format,
            NettoyerTitre(titre),
            adresse,
            Uri.TryCreate(miniature, UriKind.Absolute, out var image) ? image : null,
            publieLe);
    }

    /// <summary>
    /// Retire les mots-dièses, utiles sur YouTube mais bruyants sur le site :
    /// « Au millimètre | #ap15fr sur #Twitch » devient « Au millimètre ».
    /// Un numéro d'épisode (« #4 ») n'est pas un mot-dièse et reste.
    /// </summary>
    public static string NettoyerTitre(string titre)
    {
        var barre = titre.IndexOf(" | ", StringComparison.Ordinal);
        var utile = barre > 0 && MotDiese().IsMatch(titre[barre..]) ? titre[..barre] : titre;

        var propre = Espaces().Replace(MotDiese().Replace(utile, " "), " ").Trim();
        return propre.Length > 0 ? propre : titre.Trim();
    }

    [GeneratedRegex(@"#\p{L}[\p{L}\p{N}_]*", RegexOptions.None, matchTimeoutMilliseconds: 200)]
    private static partial Regex MotDiese();

    [GeneratedRegex(@"\s{2,}", RegexOptions.None, matchTimeoutMilliseconds: 200)]
    private static partial Regex Espaces();
}
