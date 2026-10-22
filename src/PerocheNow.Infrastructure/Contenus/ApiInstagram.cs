using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PerocheNow.Business.Contenus;
using PerocheNow.Business.Ports;
using PerocheNow.Domain.Commun;

namespace PerocheNow.Infrastructure.Contenus;

/// <summary>
/// Adaptateur de l'API Instagram (connexion Instagram, compte professionnel) :
/// <c>/me</c> pour vérifier un jeton, <c>/refresh_access_token</c> pour le prolonger,
/// <c>/me/media</c> pour lire les publications.
/// </summary>
public sealed class ApiInstagram(IHttpClientFactory fabrique, Uri adresseApi, int nombreDePublications) : IApiInstagram
{
    public const string NomClient = "Instagram";

    private const string ChampsDesPublications =
        "id,caption,media_type,media_product_type,media_url,thumbnail_url,permalink,timestamp";

    public async Task<string?> IdentifierCompteAsync(string jeton, CancellationToken annulation)
    {
        using var reponse = await Client().GetAsync(Adresse("me", $"fields=username&access_token={Echapper(jeton)}"), annulation);
        if (!reponse.IsSuccessStatusCode)
            return null;

        var compte = await reponse.Content.ReadFromJsonAsync<ReponseCompte>(annulation);
        return string.IsNullOrWhiteSpace(compte?.Username) ? null : compte.Username;
    }

    public async Task<JetonInstagram> RenouvelerAsync(string jeton, CancellationToken annulation)
    {
        var adresse = Adresse("refresh_access_token", $"grant_type=ig_refresh_token&access_token={Echapper(jeton)}");
        using var reponse = await Client().GetAsync(adresse, annulation);
        reponse.EnsureSuccessStatusCode();

        var corps = await reponse.Content.ReadFromJsonAsync<ReponseJeton>(annulation);
        if (corps is null || string.IsNullOrWhiteSpace(corps.AccessToken))
            throw new InvalidOperationException("Instagram n'a pas renvoyé de nouveau jeton.");

        return new JetonInstagram(corps.AccessToken, TimeSpan.FromSeconds(corps.ExpiresIn));
    }

    public async Task<IReadOnlyList<Contenu>> ObtenirPublicationsAsync(string jeton, CancellationToken annulation)
    {
        var requete = $"fields={ChampsDesPublications}&limit={nombreDePublications}&access_token={Echapper(jeton)}";
        using var reponse = await Client().GetAsync(Adresse("me/media", requete), annulation);
        reponse.EnsureSuccessStatusCode();

        var corps = await reponse.Content.ReadFromJsonAsync<ReponsePublications>(annulation);
        return (corps?.Data ?? [])
            .Select(Convertir)
            .OfType<Contenu>()
            .ToList();
    }

    private static Contenu? Convertir(Publication publication)
    {
        if (!Uri.TryCreate(publication.Permalink, UriKind.Absolute, out var lien) || !LireDate(publication.Timestamp, out var publieLe))
            return null;

        var video = string.Equals(publication.MediaType, "VIDEO", StringComparison.Ordinal);
        var reel = string.Equals(publication.MediaProductType, "REELS", StringComparison.Ordinal);
        var miniature = video ? publication.ThumbnailUrl : publication.MediaUrl;

        return new Contenu(
            Plateforme.Instagram,
            video || reel ? FormatDeContenu.Court : FormatDeContenu.Publication,
            Titres.DepuisLegende(publication.Caption, video || reel ? "Reel Instagram" : "Publication Instagram"),
            lien,
            Uri.TryCreate(miniature, UriKind.Absolute, out var image) ? image : null,
            publieLe)
        {
            Etiquettes = Titres.Etiquettes(publication.Caption),
        };
    }

    /// <summary>Instagram écrit le décalage horaire sans deux-points (« +0000 ») : on le remet avant de lire la date.</summary>
    private static bool LireDate(string? horodatage, out DateTimeOffset date)
    {
        var texte = horodatage is { Length: > 5 } && horodatage[^5] is '+' or '-'
            ? string.Concat(horodatage.AsSpan(0, horodatage.Length - 2), ":", horodatage.AsSpan(horodatage.Length - 2))
            : horodatage;
        return DateTimeOffset.TryParse(texte, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private HttpClient Client() => fabrique.CreateClient(NomClient);

    private Uri Adresse(string chemin, string requete) =>
        new UriBuilder(adresseApi) { Path = chemin, Query = requete }.Uri;

    private static string Echapper(string valeur) => Uri.EscapeDataString(valeur);

    private sealed record ReponseCompte([property: JsonPropertyName("username")] string? Username);

    private sealed record ReponseJeton(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")] long ExpiresIn);

    private sealed record ReponsePublications([property: JsonPropertyName("data")] IReadOnlyList<Publication>? Data);

    private sealed record Publication(
        [property: JsonPropertyName("caption")] string? Caption,
        [property: JsonPropertyName("media_type")] string? MediaType,
        [property: JsonPropertyName("media_product_type")] string? MediaProductType,
        [property: JsonPropertyName("media_url")] string? MediaUrl,
        [property: JsonPropertyName("thumbnail_url")] string? ThumbnailUrl,
        [property: JsonPropertyName("permalink")] string? Permalink,
        [property: JsonPropertyName("timestamp")] string? Timestamp);
}

/// <summary>Tant que la section <c>Contenus:Instagram</c> n'est pas configurée : Instagram reste débranché.</summary>
public sealed class ApiInstagramNonConfiguree : IApiInstagram
{
    private const string Message = "Instagram n'est pas configuré sur le site (section Contenus:Instagram).";

    public Task<string?> IdentifierCompteAsync(string jeton, CancellationToken annulation) =>
        throw new RegleMetierException(Message);

    public Task<JetonInstagram> RenouvelerAsync(string jeton, CancellationToken annulation) =>
        throw new RegleMetierException(Message);

    public Task<IReadOnlyList<Contenu>> ObtenirPublicationsAsync(string jeton, CancellationToken annulation) =>
        Task.FromResult<IReadOnlyList<Contenu>>([]);
}
