using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using PerocheNow.Business.Contenus;
using PerocheNow.Business.Ports;
using PerocheNow.Infrastructure.Contenus;

namespace PerocheNow.Infrastructure.Tests.Contenus;

public sealed class SourcesDeContenusTests
{
    private static readonly Uri AdresseFlux = new UriBuilder(Uri.UriSchemeHttps, "youtube.test") { Path = "feeds/videos.xml" }.Uri;

    // Extrait d'un vrai flux de chaîne : un Short, puis une vidéo longue.
    private const string Flux = """
        <?xml version="1.0" encoding="UTF-8"?>
        <feed xmlns:yt="http://www.youtube.com/xml/schemas/2015" xmlns:media="http://search.yahoo.com/mrss/" xmlns="http://www.w3.org/2005/Atom">
         <title>AP15</title>
         <entry>
          <id>yt:video:KQp-ygU6UCc</id>
          <yt:videoId>KQp-ygU6UCc</yt:videoId>
          <title>Crash spectaculaire sur le circuit : l'attente et l'adrénaline ! #shorts</title>
          <link rel="alternate" href="https://www.youtube.com/shorts/KQp-ygU6UCc"/>
          <published>2026-10-22T10:32:33+00:00</published>
          <media:group>
           <media:thumbnail url="https://i4.ytimg.com/vi/KQp-ygU6UCc/hqdefault.jpg" width="480" height="360"/>
          </media:group>
         </entry>
         <entry>
          <id>yt:video:C22P9v49xVo</id>
          <yt:videoId>C22P9v49xVo</yt:videoId>
          <title>MA PREMIERE FOIS SUR LA NORDSCHLEIFE !</title>
          <link rel="alternate" href="https://www.youtube.com/watch?v=C22P9v49xVo"/>
          <published>2026-08-30T14:15:28+00:00</published>
          <media:group>
           <media:thumbnail url="https://i2.ytimg.com/vi/C22P9v49xVo/hqdefault.jpg" width="480" height="360"/>
          </media:group>
         </entry>
        </feed>
        """;

    [Fact]
    public async Task Le_flux_YouTube_devient_des_contenus_classes_par_format()
    {
        using var youtube = new FauxServeur(Flux);

        var contenus = await new SourceYouTube(new FabriqueDeClients(youtube), AdresseFlux).ObtenirAsync(CancellationToken.None);

        Assert.Collection(contenus,
            court =>
            {
                Assert.Equal(FormatDeContenu.Court, court.Format);
                Assert.Equal("Crash spectaculaire sur le circuit : l'attente et l'adrénaline !", court.Titre);
                Assert.Equal("/shorts/KQp-ygU6UCc", court.Lien.AbsolutePath);
                Assert.Equal("/vi/KQp-ygU6UCc/hqdefault.jpg", court.Miniature?.AbsolutePath);
                Assert.Equal(new DateTimeOffset(2026, 10, 22, 10, 32, 33, TimeSpan.Zero), court.PublieLe);
            },
            video =>
            {
                Assert.Equal(FormatDeContenu.Video, video.Format);
                Assert.Equal(Plateforme.YouTube, video.Plateforme);
                Assert.Equal("MA PREMIERE FOIS SUR LA NORDSCHLEIFE !", video.Titre);
            });
    }

    [Theory]
    [InlineData("Au milimètre | #ap15fr sur #Twitch", "Au milimètre")]
    [InlineData("3 galères, mais demain ça ira mieux ! 🚗✨ #shorts", "3 galères, mais demain ça ira mieux ! 🚗✨")]
    [InlineData("Toujours se méfier des 106", "Toujours se méfier des 106")]
    [InlineData("#shorts", "#shorts")]
    [InlineData("ON ACCOMPAGNE @guillaumefayepilote  CHEZ AGS | RACINGVLOG #4", "ON ACCOMPAGNE @guillaumefayepilote CHEZ AGS | RACINGVLOG #4")]
    [InlineData("Touristenfahrten Onboard - Golf GTI 7.5 #8", "Touristenfahrten Onboard - Golf GTI 7.5 #8")]
    public void Les_mots_dieses_sont_retires_des_titres(string titre, string attendu)
    {
        Assert.Equal(attendu, Titres.Nettoyer(titre));
    }

    [Fact]
    public async Task Le_cache_evite_de_relire_la_plateforme()
    {
        var source = new SourceComptee();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var decoree = new SourceAvecCache(source, cache, NullLogger<SourceAvecCache>.Instance, "test");

        await decoree.ObtenirAsync(CancellationToken.None);
        var deuxieme = await decoree.ObtenirAsync(CancellationToken.None);

        Assert.Equal(1, source.Appels);
        Assert.Single(deuxieme);
    }

    [Fact]
    public async Task Une_plateforme_en_panne_rend_les_derniers_contenus_connus()
    {
        var source = new SourceComptee();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var decoree = new SourceAvecCache(source, cache, NullLogger<SourceAvecCache>.Instance, "test");

        await decoree.ObtenirAsync(CancellationToken.None);
        cache.Remove("test");
        source.EnPanne = true;
        var apresPanne = await decoree.ObtenirAsync(CancellationToken.None);

        Assert.Equal(2, source.Appels);
        Assert.Single(apresPanne);
    }

    private sealed class SourceComptee : ISourceDeContenus
    {
        public int Appels { get; private set; }
        public bool EnPanne { get; set; }

        public Task<IReadOnlyList<Contenu>> ObtenirAsync(CancellationToken annulation)
        {
            Appels++;
            if (EnPanne)
                throw new HttpRequestException("Plateforme indisponible");

            IReadOnlyList<Contenu> contenus =
            [
                new(Plateforme.YouTube, FormatDeContenu.Video, "Vidéo", AdresseFlux, null, DateTimeOffset.UnixEpoch),
            ];
            return Task.FromResult(contenus);
        }
    }

    private sealed class FauxServeur(string corps) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(corps, Encoding.UTF8, "application/atom+xml"),
            });
    }

    private sealed class FabriqueDeClients(HttpMessageHandler gestionnaire) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(gestionnaire, disposeHandler: false);
    }
}
