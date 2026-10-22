using System.Globalization;
using System.Xml.Linq;
using PerocheNow.Business.Contenus;
using PerocheNow.Business.Ports;

namespace PerocheNow.Infrastructure.Contenus;

/// <summary>
/// Adaptateur YouTube : lit un flux Atom public (aucune clé d'API) et le traduit en <see cref="Contenu"/>.
/// Un lien en <c>/shorts/</c> signale un format court, tout autre lien une vidéo.
/// </summary>
public sealed class SourceYouTube(IHttpClientFactory fabrique, Uri adresseFlux) : ISourceDeContenus
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
            Titres.Nettoyer(titre),
            adresse,
            Uri.TryCreate(miniature, UriKind.Absolute, out var image) ? image : null,
            publieLe)
        {
            Etiquettes = Titres.Etiquettes(titre),
        };
    }
}
