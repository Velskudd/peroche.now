using System.Net;
using System.Text;
using PerocheNow.Business.Ports;
using PerocheNow.Infrastructure.Twitch;

namespace PerocheNow.Infrastructure.Tests.Twitch;

public sealed class SourceDuDirectTwitchTests
{
    private const string HoteApi = "api.twitch.test";
    private const string HoteJeton = "id.twitch.test";

    private static readonly DateTimeOffset Maintenant = new(2026, 10, 20, 19, 0, 0, TimeSpan.Zero);

    private static readonly ParametresTwitch Parametres = new(
        "ap15fr",
        "identifiant",
        "secret",
        new UriBuilder(Uri.UriSchemeHttps, HoteApi) { Path = "helix/streams" }.Uri,
        new UriBuilder(Uri.UriSchemeHttps, HoteJeton) { Path = "oauth2/token" }.Uri);

    [Fact]
    public async Task Hors_direct_la_source_ne_renvoie_rien()
    {
        using var twitch = new FauxTwitch { CorpsFlux = """{"data":[],"pagination":{}}""" };

        Assert.Null(await Source(twitch).ObtenirAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Pendant_un_direct_la_source_renvoie_titre_jeu_et_debut()
    {
        using var twitch = new FauxTwitch
        {
            CorpsFlux = """
                {"data":[{"user_login":"ap15fr","type":"live","title":"NEC, manche 9","game_name":"iRacing","started_at":"2026-10-20T18:30:00Z"}]}
                """,
        };

        var direct = await Source(twitch).ObtenirAsync(CancellationToken.None);

        Assert.Equal(new DirectEnCours("NEC, manche 9", "iRacing", new DateTimeOffset(2026, 10, 20, 18, 30, 0, TimeSpan.Zero)), direct);
    }

    [Fact]
    public async Task La_requete_vise_la_chaine_avec_l_identifiant_et_le_jeton()
    {
        using var twitch = new FauxTwitch();

        await Source(twitch).ObtenirAsync(CancellationToken.None);

        var appel = Assert.Single(twitch.AppelsFlux);
        Assert.Equal("?user_login=ap15fr", appel.Query);
        Assert.Equal("identifiant", appel.IdentifiantClient);
        Assert.Equal("jeton-1", appel.JetonPresente);
    }

    [Fact]
    public async Task Le_jeton_est_garde_en_memoire_entre_deux_appels()
    {
        using var twitch = new FauxTwitch();
        var jeton = Jeton(twitch);

        await Source(twitch, jeton).ObtenirAsync(CancellationToken.None);
        await Source(twitch, jeton).ObtenirAsync(CancellationToken.None);

        Assert.Equal(1, twitch.DemandesDeJeton);
        Assert.Equal(2, twitch.AppelsFlux.Count);
    }

    [Fact]
    public async Task Un_jeton_refuse_est_redemande_une_seule_fois()
    {
        using var twitch = new FauxTwitch();
        twitch.StatutsFlux.Enqueue(HttpStatusCode.Unauthorized);

        await Source(twitch).ObtenirAsync(CancellationToken.None);

        Assert.Equal(2, twitch.DemandesDeJeton);
        Assert.Equal(new[] { "jeton-1", "jeton-2" }, twitch.AppelsFlux.Select(a => a.JetonPresente).ToArray());
    }

    [Fact]
    public void Sans_identifiants_la_configuration_est_incomplete()
    {
        var options = new TwitchOptions
        {
            Chaine = "ap15fr",
            AdresseFlux = Parametres.AdresseFlux,
            AdresseJeton = Parametres.AdresseJeton,
        };

        Assert.Null(options.Parametres());
    }

    [Fact]
    public void Le_secret_n_apparait_jamais_dans_le_texte_des_parametres()
    {
        Assert.DoesNotContain("secret", Parametres.ToString(), StringComparison.Ordinal);
    }

    private static JetonTwitch Jeton(FauxTwitch twitch) =>
        new(new FabriqueDeClients(twitch), Parametres, new HorlogeFixe(Maintenant));

    private static SourceDuDirectTwitch Source(FauxTwitch twitch, JetonTwitch? jeton = null) =>
        new(new FabriqueDeClients(twitch), jeton ?? Jeton(twitch), Parametres);

    private sealed record AppelFlux(string Query, string IdentifiantClient, string JetonPresente);

    /// <summary>Simule l'API Twitch : un jeton numéroté par demande, et la liste des directs.</summary>
    private sealed class FauxTwitch : HttpMessageHandler
    {
        public string CorpsFlux { get; init; } = """{"data":[]}""";
        public Queue<HttpStatusCode> StatutsFlux { get; } = new();
        public List<AppelFlux> AppelsFlux { get; } = [];
        public int DemandesDeJeton { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var adresse = request.RequestUri!;
            if (adresse.Host == HoteJeton)
            {
                DemandesDeJeton++;
                return Task.FromResult(Json($$"""{"access_token":"jeton-{{DemandesDeJeton}}","expires_in":5000000,"token_type":"bearer"}"""));
            }

            var identifiant = request.Headers.TryGetValues("Client-Id", out var valeurs) ? valeurs.Single() : string.Empty;
            AppelsFlux.Add(new AppelFlux(adresse.Query, identifiant, request.Headers.Authorization?.Parameter ?? string.Empty));

            var statut = StatutsFlux.Count > 0 ? StatutsFlux.Dequeue() : HttpStatusCode.OK;
            return Task.FromResult(statut == HttpStatusCode.OK ? Json(CorpsFlux) : new HttpResponseMessage(statut));
        }

        private static HttpResponseMessage Json(string corps) =>
            new(HttpStatusCode.OK) { Content = new StringContent(corps, Encoding.UTF8, "application/json") };
    }

    private sealed class FabriqueDeClients(HttpMessageHandler gestionnaire) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(gestionnaire, disposeHandler: false);
    }

    private sealed class HorlogeFixe(DateTimeOffset instant) : IHorloge
    {
        public DateTimeOffset Instant { get; } = instant;
    }
}
