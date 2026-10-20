using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PerocheNow.Business.Ports;

namespace PerocheNow.Infrastructure.Twitch;

/// <summary>
/// Adaptateur Twitch du port <see cref="ISourceDuDirect"/> : interroge l'API Helix
/// (<c>GET /helix/streams?user_login=…</c>), qui ne renvoie un flux que pendant un direct.
/// </summary>
public sealed class SourceDuDirectTwitch(IHttpClientFactory fabrique, JetonTwitch jeton, ParametresTwitch parametres)
    : ISourceDuDirect
{
    public const string NomClient = "Twitch";

    public async Task<DirectEnCours?> ObtenirAsync(CancellationToken annulation)
    {
        var reponse = await LireFluxAsync(annulation);
        var flux = reponse?.Data?.FirstOrDefault(f => string.Equals(f.Type, "live", StringComparison.Ordinal));
        if (flux is null)
            return null;

        var jeu = string.IsNullOrWhiteSpace(flux.GameName) ? null : flux.GameName;
        return new DirectEnCours(flux.Title ?? string.Empty, jeu, flux.StartedAt);
    }

    private async Task<ReponseFlux?> LireFluxAsync(CancellationToken annulation)
    {
        using (var premiere = await EnvoyerAsync(annulation))
        {
            if (premiere.StatusCode != HttpStatusCode.Unauthorized)
                return await LireCorpsAsync(premiere, annulation);
        }

        // Jeton refusé avant son expiration (révoqué, secret changé) : on en redemande un, une seule fois.
        jeton.Oublier();
        using var seconde = await EnvoyerAsync(annulation);
        return await LireCorpsAsync(seconde, annulation);
    }

    private async Task<HttpResponseMessage> EnvoyerAsync(CancellationToken annulation)
    {
        var valeur = await jeton.ObtenirAsync(annulation);
        var adresse = new UriBuilder(parametres.AdresseFlux)
        {
            Query = "user_login=" + Uri.EscapeDataString(parametres.Chaine),
        }.Uri;

        using var requete = new HttpRequestMessage(HttpMethod.Get, adresse);
        requete.Headers.Add("Client-Id", parametres.IdentifiantClient);
        requete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", valeur);
        return await fabrique.CreateClient(NomClient).SendAsync(requete, annulation);
    }

    private static async Task<ReponseFlux?> LireCorpsAsync(HttpResponseMessage reponse, CancellationToken annulation)
    {
        reponse.EnsureSuccessStatusCode();
        return await reponse.Content.ReadFromJsonAsync<ReponseFlux>(annulation);
    }

    private sealed record ReponseFlux(
        [property: JsonPropertyName("data")] IReadOnlyList<Flux>? Data);

    private sealed record Flux(
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("game_name")] string? GameName,
        [property: JsonPropertyName("started_at")] DateTimeOffset StartedAt);
}

/// <summary>Tant que Twitch n'est pas configuré : jamais en direct, sans appel réseau.</summary>
public sealed class SourceDuDirectInactive : ISourceDuDirect
{
    public Task<DirectEnCours?> ObtenirAsync(CancellationToken annulation) => Task.FromResult<DirectEnCours?>(null);
}
