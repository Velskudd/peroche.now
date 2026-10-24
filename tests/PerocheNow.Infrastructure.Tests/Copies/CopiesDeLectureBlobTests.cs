using System.Net;
using System.Net.Http.Headers;
using System.Text;
using PerocheNow.Business.Maintenant;
using PerocheNow.Business.Ports;
using PerocheNow.Domain.Maintenant;
using PerocheNow.Infrastructure.Copies;

namespace PerocheNow.Infrastructure.Tests.Copies;

public sealed class CopiesDeLectureBlobTests
{
    private const string Compte = "perochetest";
    private static readonly byte[] Cle = Encoding.UTF8.GetBytes("cle-de-test-peroche");
    private static readonly DateTimeOffset MomentFixe = new(2026, 10, 24, 16, 0, 0, TimeSpan.Zero);
    private static readonly Uri Conteneur = new UriBuilder(Uri.UriSchemeHttps, $"{Compte}.blob.core.windows.test") { Path = "copies" }.Uri;

    // Vecteurs calculés à part (HMAC-SHA256 en Python) à partir de la chaîne décrite par la documentation Azure.
    private const string SignatureListe = "HLwjQeFZC4XIsN/2ufq5Is0Q/W6zk94TfCmlsnwJ59I=";
    private const string SignatureDepot = "KHNqeY+h9jNT79Jd+i7M/fDtlPQRPfb/4JtrOIiRvUA=";

    [Fact]
    public void La_signature_suit_la_regle_Azure_avec_les_parametres_tries()
    {
        using var requete = new HttpRequestMessage(HttpMethod.Get, new Uri(Conteneur, "?restype=container&comp=list"));

        SignatureCleePartagee.Signer(requete, Compte, Cle, MomentFixe);

        Assert.Equal(
            "GET" + new string('\n', 12)
            + "x-ms-date:Sat, 24 Oct 2026 16:00:00 GMT\nx-ms-version:2023-11-03\n"
            + "/perochetest/copies\ncomp:list\nrestype:container",
            SignatureCleePartagee.ChaineASigner(requete, Compte));
        Assert.Equal("SharedKey", requete.Headers.Authorization?.Scheme);
        Assert.Equal($"{Compte}:{SignatureListe}", requete.Headers.Authorization?.Parameter);
    }

    [Fact]
    public void Un_depot_signe_sa_longueur_et_son_type()
    {
        using var requete = new HttpRequestMessage(HttpMethod.Put, new Uri($"{Conteneur}/statut-du-moment.json"))
        {
            Content = new ByteArrayContent("""{"a":1}"""u8.ToArray()),
        };
        requete.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        requete.Headers.TryAddWithoutValidation("x-ms-blob-type", "BlockBlob");

        SignatureCleePartagee.Signer(requete, Compte, Cle, MomentFixe);

        Assert.Equal($"{Compte}:{SignatureDepot}", requete.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task Une_copie_absente_se_lit_comme_rien()
    {
        var stockage = new FauxStockage();

        Assert.Null(await Copies(stockage).LireAsync<StatutDuMomentDto>("absente.json", CancellationToken.None));
    }

    [Fact]
    public async Task Une_copie_publiee_se_relit_a_l_identique()
    {
        var stockage = new FauxStockage();
        var copies = Copies(stockage);
        var statut = new StatutDuMomentDto(
            "Je prépare ma 1re course sur la Nordschleife",
            [new LigneDuMomentDto("Sur iRacing, en NEC.", Rubrique.ProjetEnCours), new LigneDuMomentDto("Deux lives par semaine.", Rubrique.Contenus)],
            new ObjectifDto("RCN 2027", new DateOnly(2027, 9, 1)),
            MomentFixe);

        await copies.PublierAsync("statut-du-moment.json", statut, CancellationToken.None);
        var relu = await copies.LireAsync<StatutDuMomentDto>("statut-du-moment.json", CancellationToken.None);

        Assert.NotNull(relu);
        Assert.Equal(statut.Accroche, relu.Accroche);
        Assert.Equal(statut.Lignes, relu.Lignes);
        Assert.Equal(statut.ProchainObjectif, relu.ProchainObjectif);
        Assert.Equal(statut.MisAJourLe, relu.MisAJourLe);

        // Les rubriques sont écrites en toutes lettres : la copie reste lisible depuis le portail Azure.
        Assert.Contains("\"ProjetEnCours\"", stockage.Contenu("/copies/statut-du-moment.json"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Un_depot_est_un_blob_de_blocs_signe_avec_la_cle_du_compte()
    {
        var stockage = new FauxStockage();

        await Copies(stockage).PublierAsync("etat.json", new { Version = 1 }, CancellationToken.None);

        var depot = Assert.Single(stockage.Requetes);
        Assert.Equal(HttpMethod.Put, depot.Methode);
        Assert.Equal("BlockBlob", depot.EnTetes["x-ms-blob-type"]);
        Assert.Equal(SignatureCleePartagee.VersionApi, depot.EnTetes["x-ms-version"]);
        Assert.Equal("Sat, 24 Oct 2026 16:00:00 GMT", depot.EnTetes["x-ms-date"]);
        Assert.StartsWith($"SharedKey {Compte}:", depot.EnTetes["Authorization"], StringComparison.Ordinal);
    }

    [Fact]
    public async Task Un_refus_du_stockage_remonte_en_erreur()
    {
        var stockage = new FauxStockage { Statut = HttpStatusCode.Forbidden };

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            Copies(stockage).LireAsync<StatutDuMomentDto>("statut-du-moment.json", CancellationToken.None));
    }

    [Fact]
    public void Sans_reglage_complet_il_n_y_a_pas_de_copie()
    {
        Assert.Null(new CopiesOptions { AdresseConteneur = Conteneur, Compte = Compte }.Parametres());
        Assert.NotNull(new CopiesOptions { AdresseConteneur = Conteneur, Compte = Compte, Cle = Convert.ToBase64String(Cle) }.Parametres());
        Assert.DoesNotContain(Convert.ToBase64String(Cle), new ParametresCopies(Conteneur, Compte, Cle).ToString(), StringComparison.Ordinal);
    }

    private static CopiesDeLectureBlob Copies(FauxStockage stockage) =>
        new(new FabriqueDeClients(stockage), new ParametresCopies(Conteneur, Compte, Cle), new HorlogeFixe(MomentFixe));

    private sealed record RequeteRecue(HttpMethod Methode, string Chemin, IReadOnlyDictionary<string, string> EnTetes);

    /// <summary>Un conteneur Blob en mémoire : garde ce qu'on y dépose et le rend à la lecture.</summary>
    private sealed class FauxStockage : HttpMessageHandler
    {
        private readonly Dictionary<string, byte[]> _blobs = [];

        public HttpStatusCode? Statut { get; init; }
        public List<RequeteRecue> Requetes { get; } = [];

        public string Contenu(string chemin) => Encoding.UTF8.GetString(_blobs[chemin]);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var chemin = request.RequestUri!.AbsolutePath;
            Requetes.Add(new RequeteRecue(
                request.Method,
                chemin,
                request.Headers.ToDictionary(entete => entete.Key, entete => string.Join(',', entete.Value), StringComparer.OrdinalIgnoreCase)));

            if (Statut is { } statut)
                return new HttpResponseMessage(statut);

            if (request.Method == HttpMethod.Put)
            {
                _blobs[chemin] = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.Created);
            }

            return _blobs.TryGetValue(chemin, out var octets)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(octets) }
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        }
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
