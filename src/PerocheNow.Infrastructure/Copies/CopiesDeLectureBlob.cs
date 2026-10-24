using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PerocheNow.Business.Ports;

namespace PerocheNow.Infrastructure.Copies;

/// <summary>
/// Adaptateur Azure Blob des copies de lecture : un document JSON par copie, dans un conteneur privé.
/// Le versionnage du compte de stockage garde automatiquement chaque version remplacée.
/// </summary>
public sealed class CopiesDeLectureBlob(IHttpClientFactory fabrique, ParametresCopies parametres, IHorloge horloge)
    : ICopiesDeLecture
{
    public const string NomClient = "CopiesDeLecture";

    private static readonly JsonSerializerOptions OptionsJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<T?> LireAsync<T>(string nom, CancellationToken annulation) where T : class
    {
        using var requete = new HttpRequestMessage(HttpMethod.Get, Adresse(nom));
        SignatureCleePartagee.Signer(requete, parametres.Compte, parametres.Cle, horloge.Instant);

        using var reponse = await Client().SendAsync(requete, annulation);
        if (reponse.StatusCode == HttpStatusCode.NotFound)
            return null;

        reponse.EnsureSuccessStatusCode();
        return await reponse.Content.ReadFromJsonAsync<T>(OptionsJson, annulation);
    }

    public async Task PublierAsync<T>(string nom, T copie, CancellationToken annulation) where T : class
    {
        using var requete = new HttpRequestMessage(HttpMethod.Put, Adresse(nom))
        {
            Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(copie, OptionsJson)),
        };
        requete.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        requete.Headers.TryAddWithoutValidation("x-ms-blob-type", "BlockBlob");
        SignatureCleePartagee.Signer(requete, parametres.Compte, parametres.Cle, horloge.Instant);

        using var reponse = await Client().SendAsync(requete, annulation);
        reponse.EnsureSuccessStatusCode();
    }

    private HttpClient Client() => fabrique.CreateClient(NomClient);

    private Uri Adresse(string nom) =>
        new($"{parametres.AdresseConteneur.AbsoluteUri.TrimEnd('/')}/{Uri.EscapeDataString(nom)}");
}

/// <summary>Sans conteneur configuré (en local) : aucune copie, le site lit la base directement.</summary>
public sealed class CopiesDeLectureNonConfigurees : ICopiesDeLecture
{
    public Task<T?> LireAsync<T>(string nom, CancellationToken annulation) where T : class =>
        Task.FromResult<T?>(null);

    public Task PublierAsync<T>(string nom, T copie, CancellationToken annulation) where T : class =>
        Task.CompletedTask;
}
