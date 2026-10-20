using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PerocheNow.Business.Ports;

namespace PerocheNow.Infrastructure.Twitch;

/// <summary>
/// Le jeton d'application Twitch (flux « client credentials »). Il vaut plusieurs semaines :
/// on le garde en mémoire et on n'en redemande un qu'à son expiration, ou quand Twitch le refuse.
/// </summary>
public sealed class JetonTwitch(IHttpClientFactory fabrique, ParametresTwitch parametres, IHorloge horloge) : IDisposable
{
    /// <summary>On n'utilise jamais un jeton à quelques minutes de son expiration.</summary>
    private static readonly TimeSpan Marge = TimeSpan.FromMinutes(5);

    private readonly SemaphoreSlim _verrou = new(1, 1);
    private volatile Jeton? _jeton;

    public async Task<string> ObtenirAsync(CancellationToken annulation)
    {
        if (Valide(_jeton) is { } enMemoire)
            return enMemoire;

        await _verrou.WaitAsync(annulation);
        try
        {
            // Une autre requête a peut-être obtenu le jeton pendant l'attente du verrou.
            if (Valide(_jeton) is { } obtenuEntreTemps)
                return obtenuEntreTemps;

            var nouveau = await DemanderAsync(annulation);
            _jeton = nouveau;
            return nouveau.Valeur;
        }
        finally
        {
            _verrou.Release();
        }
    }

    /// <summary>Twitch a refusé le jeton (révoqué, secret changé) : le prochain appel en redemandera un.</summary>
    public void Oublier() => _jeton = null;

    public void Dispose() => _verrou.Dispose();

    private string? Valide(Jeton? jeton) =>
        jeton is not null && horloge.Instant < jeton.ExpireLe ? jeton.Valeur : null;

    private async Task<Jeton> DemanderAsync(CancellationToken annulation)
    {
        using var formulaire = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = parametres.IdentifiantClient,
            ["client_secret"] = parametres.SecretClient,
            ["grant_type"] = "client_credentials",
        });

        using var reponse = await fabrique.CreateClient(SourceDuDirectTwitch.NomClient)
            .PostAsync(parametres.AdresseJeton, formulaire, annulation);
        reponse.EnsureSuccessStatusCode();

        var corps = await reponse.Content.ReadFromJsonAsync<ReponseJeton>(annulation)
            ?? throw new InvalidOperationException("Twitch n'a pas renvoyé de jeton.");
        return new Jeton(corps.AccessToken, horloge.Instant.AddSeconds(corps.ExpiresIn) - Marge);
    }

    private sealed record Jeton(string Valeur, DateTimeOffset ExpireLe);

    private sealed record ReponseJeton(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] long ExpiresIn);
}
