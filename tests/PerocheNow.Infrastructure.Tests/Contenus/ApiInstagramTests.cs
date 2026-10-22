using System.Net;
using System.Text;
using PerocheNow.Business.Contenus;
using PerocheNow.Infrastructure.Contenus;

namespace PerocheNow.Infrastructure.Tests.Contenus;

public sealed class ApiInstagramTests
{
    private static readonly Uri AdresseApi = new UriBuilder(Uri.UriSchemeHttps, "graph.instagram.test").Uri;

    private const string Publications = """
        {"data":[
          {"id":"1","caption":"Semaine 3 : premier roulage sur la Nordschleife\n\n#NUR24 #nordschleife","media_type":"VIDEO","media_product_type":"REELS",
           "media_url":"https://scontent.cdninstagram.test/v/reel.mp4","thumbnail_url":"https://scontent.cdninstagram.test/v/reel.jpg",
           "permalink":"https://www.instagram.com/reel/AbC/","timestamp":"2026-10-20T18:30:00+0000"},
          {"id":"2","caption":"#paddock","media_type":"CAROUSEL_ALBUM","media_product_type":"FEED",
           "media_url":"https://scontent.cdninstagram.test/v/photo.jpg","permalink":"https://www.instagram.com/p/XyZ/","timestamp":"2026-10-18T09:00:00+0000"},
          {"id":"3","caption":"Sans lien","media_type":"IMAGE","media_product_type":"FEED","timestamp":"2026-10-17T09:00:00+0000"}
        ]}
        """;

    [Fact]
    public async Task Les_publications_deviennent_des_contenus_avec_leur_format_et_leurs_etiquettes()
    {
        using var instagram = new FauxInstagram(HttpStatusCode.OK, Publications);

        var contenus = await Api(instagram).ObtenirPublicationsAsync("IGAAjeton", CancellationToken.None);

        Assert.Collection(contenus,
            reel =>
            {
                Assert.Equal(Plateforme.Instagram, reel.Plateforme);
                Assert.Equal(FormatDeContenu.Court, reel.Format);
                Assert.Equal("Semaine 3 : premier roulage sur la Nordschleife", reel.Titre);
                Assert.Equal("/v/reel.jpg", reel.Miniature?.AbsolutePath);
                Assert.Equal(new DateTimeOffset(2026, 10, 20, 18, 30, 0, TimeSpan.Zero), reel.PublieLe);
                Assert.True(reel.PorteEtiquette("nur24"));
                Assert.True(reel.PorteEtiquette("#Nordschleife"));
            },
            carrousel =>
            {
                Assert.Equal(FormatDeContenu.Publication, carrousel.Format);
                Assert.Equal("Publication Instagram", carrousel.Titre);
                Assert.Equal("/v/photo.jpg", carrousel.Miniature?.AbsolutePath);
                Assert.False(carrousel.PorteEtiquette("NUR24"));
            });
        Assert.Contains("access_token=IGAAjeton", instagram.DerniereRequete);
    }

    [Fact]
    public async Task Un_jeton_refuse_ne_donne_aucun_compte()
    {
        using var instagram = new FauxInstagram(HttpStatusCode.BadRequest, """{"error":{"code":190}}""");

        Assert.Null(await Api(instagram).IdentifierCompteAsync("IGAAfaux", CancellationToken.None));
    }

    [Fact]
    public async Task Un_jeton_valide_donne_le_nom_du_compte()
    {
        using var instagram = new FauxInstagram(HttpStatusCode.OK, """{"username":"peroche","id":"42"}""");

        Assert.Equal("peroche", await Api(instagram).IdentifierCompteAsync("IGAAjeton", CancellationToken.None));
        Assert.Contains("/me?fields=username", instagram.DerniereRequete);
    }

    [Fact]
    public async Task Le_renouvellement_renvoie_le_nouveau_jeton_et_sa_duree()
    {
        using var instagram = new FauxInstagram(HttpStatusCode.OK, """{"access_token":"IGAAneuf","token_type":"bearer","expires_in":5184000}""");

        var jeton = await Api(instagram).RenouvelerAsync("IGAAancien", CancellationToken.None);

        Assert.Equal("IGAAneuf", jeton.Valeur);
        Assert.Equal(TimeSpan.FromDays(60), jeton.Validite);
        Assert.Contains("grant_type=ig_refresh_token", instagram.DerniereRequete);
        Assert.DoesNotContain("IGAAneuf", jeton.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Une légende courte\nsuite", "Une légende courte")]
    [InlineData(null, "Par défaut")]
    [InlineData("#seulement #des #mots", "Par défaut")]
    public void La_legende_donne_un_titre_d_une_ligne(string? legende, string attendu)
    {
        Assert.Equal(attendu, Titres.DepuisLegende(legende, "Par défaut"));
    }

    [Fact]
    public void Une_legende_trop_longue_est_coupee_sur_un_mot()
    {
        var titre = Titres.DepuisLegende(string.Join(' ', Enumerable.Repeat("Nordschleife", 20)), "Par défaut");

        Assert.True(titre.Length <= Titres.LongueurMax + 1);
        Assert.EndsWith("Nordschleife…", titre, StringComparison.Ordinal);
    }

    private static ApiInstagram Api(FauxInstagram instagram) => new(new FabriqueDeClients(instagram), AdresseApi, 12);

    private sealed class FauxInstagram(HttpStatusCode statut, string corps) : HttpMessageHandler
    {
        public string DerniereRequete { get; private set; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            DerniereRequete = request.RequestUri?.PathAndQuery ?? string.Empty;
            return Task.FromResult(new HttpResponseMessage(statut)
            {
                Content = new StringContent(corps, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class FabriqueDeClients(HttpMessageHandler gestionnaire) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(gestionnaire, disposeHandler: false);
    }
}
