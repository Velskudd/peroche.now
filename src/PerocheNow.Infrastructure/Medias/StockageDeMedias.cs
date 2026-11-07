using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PerocheNow.Business.Ports;
using PerocheNow.Domain.Medias;
using PerocheNow.Infrastructure.Copies;

namespace PerocheNow.Infrastructure.Medias;

/// <summary>Section <c>Medias</c> de la configuration : le conteneur Azure Blob des photos et vidéos (même compte que les copies).</summary>
public sealed class MediasOptions
{
    public const string Section = "Medias";

    /// <summary>Adresse du conteneur, par exemple <c>https://compte.blob.core.windows.net/medias</c>.</summary>
    public Uri? AdresseConteneur { get; init; }
}

/// <summary>
/// Adaptateur Azure Blob des médias : un fichier par envoi, dans un conteneur privé.
/// Les visiteurs ne lisent jamais le stockage directement : le site leur sert les fichiers (voir <c>/medias/…</c>).
/// </summary>
public sealed class StockageDeMediasBlob(IHttpClientFactory fabrique, Uri adresseConteneur, ParametresCopies compte, IHorloge horloge)
    : IStockageDeMedias
{
    public const string NomClient = "Medias";

    public async Task DeposerAsync(FichierMedia fichier, ReadOnlyMemory<byte> contenu, CancellationToken annulation)
    {
        using var requete = new HttpRequestMessage(HttpMethod.Put, Adresse(fichier.Nom))
        {
            Content = new ReadOnlyMemoryContent(contenu),
        };
        requete.Content.Headers.ContentType = new MediaTypeHeaderValue(fichier.TypeContenu);
        requete.Content.Headers.ContentLength = contenu.Length;
        requete.Headers.TryAddWithoutValidation("x-ms-blob-type", "BlockBlob");
        requete.Headers.TryAddWithoutValidation("x-ms-blob-content-type", fichier.TypeContenu);
        SignatureCleePartagee.Signer(requete, compte.Compte, compte.Cle, horloge.Instant);

        using var reponse = await Client().SendAsync(requete, annulation);
        reponse.EnsureSuccessStatusCode();
    }

    public async Task<MediaOuvert?> OuvrirAsync(string nom, string? plage, CancellationToken annulation)
    {
        using var requete = new HttpRequestMessage(HttpMethod.Get, Adresse(nom));
        // La plage passe par x-ms-range : elle entre ainsi dans la signature comme les autres en-têtes Azure.
        if (!string.IsNullOrWhiteSpace(plage))
            requete.Headers.TryAddWithoutValidation("x-ms-range", plage);
        SignatureCleePartagee.Signer(requete, compte.Compte, compte.Cle, horloge.Instant);

        var reponse = await Client().SendAsync(requete, HttpCompletionOption.ResponseHeadersRead, annulation);
        if (reponse.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.RequestedRangeNotSatisfiable)
        {
            reponse.Dispose();
            return null;
        }

        try
        {
            reponse.EnsureSuccessStatusCode();
        }
        catch
        {
            reponse.Dispose();
            throw;
        }

        var contenu = reponse.Content;
        return new MediaOuvert(
            await contenu.ReadAsStreamAsync(annulation),
            contenu.Headers.ContentType?.MediaType ?? "application/octet-stream",
            contenu.Headers.ContentLength,
            reponse.StatusCode == HttpStatusCode.PartialContent ? contenu.Headers.ContentRange?.ToString() : null,
            reponse.Headers.ETag?.Tag,
            new Liberation(reponse));
    }

    private HttpClient Client() => fabrique.CreateClient(NomClient);

    private Uri Adresse(string nom) => new($"{adresseConteneur.AbsoluteUri.TrimEnd('/')}/{Uri.EscapeDataString(nom)}");

    private sealed class Liberation(IDisposable reponse) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            reponse.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

/// <summary>
/// Sans conteneur configuré (en local) : les médias envoyés restent en mémoire le temps que le site tourne.
/// Les plages ne sont pas gérées : le fichier est servi en entier.
/// </summary>
public sealed class StockageDeMediasEnMemoire : IStockageDeMedias
{
    private readonly ConcurrentDictionary<string, (string Type, byte[] Octets)> _fichiers = new(StringComparer.Ordinal);

    public Task DeposerAsync(FichierMedia fichier, ReadOnlyMemory<byte> contenu, CancellationToken annulation)
    {
        _fichiers[fichier.Nom] = (fichier.TypeContenu, contenu.ToArray());
        return Task.CompletedTask;
    }

    public Task<MediaOuvert?> OuvrirAsync(string nom, string? plage, CancellationToken annulation)
    {
        if (!_fichiers.TryGetValue(nom, out var fichier))
            return Task.FromResult<MediaOuvert?>(null);

        var flux = new MemoryStream(fichier.Octets, writable: false);
        return Task.FromResult<MediaOuvert?>(new MediaOuvert(flux, fichier.Type, fichier.Octets.Length, null, null, flux));
    }
}

public static class EnregistrementMedias
{
    /// <summary>Le conteneur Azure Blob si les sections <c>Medias</c> et <c>Copies</c> sont complètes, sinon la mémoire.</summary>
    public static IServiceCollection AjouterStockageDeMedias(this IServiceCollection services, IConfiguration configuration)
    {
        var adresse = configuration.GetSection(MediasOptions.Section).Get<MediasOptions>()?.AdresseConteneur;
        var compte = configuration.GetSection(CopiesOptions.Section).Get<CopiesOptions>()?.Parametres();
        if (adresse is null || compte is null)
        {
            services.AddSingleton<IStockageDeMedias, StockageDeMediasEnMemoire>();
            return services;
        }

        services.AddHttpClient(StockageDeMediasBlob.NomClient, client => client.Timeout = TimeSpan.FromSeconds(60));
        services.AddSingleton<IStockageDeMedias>(fournisseur => new StockageDeMediasBlob(
            fournisseur.GetRequiredService<IHttpClientFactory>(), adresse, compte, fournisseur.GetRequiredService<IHorloge>()));
        return services;
    }
}
