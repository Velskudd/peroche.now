using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PerocheNow.Business.Ports;

namespace PerocheNow.Infrastructure.Contenus;

/// <summary>Section <c>Contenus</c> de la configuration : les flux à lire, plateforme par plateforme.</summary>
public sealed class ContenusOptions
{
    public const string Section = "Contenus";

    public FluxYouTubeOptions YouTube { get; init; } = new();
    public InstagramOptions Instagram { get; init; } = new();
}

/// <summary>
/// L'API Instagram (connexion Instagram, compte professionnel). Le jeton, lui, n'est pas dans la configuration :
/// il est saisi dans l'administration, gardé en base et renouvelé par le site.
/// </summary>
public sealed class InstagramOptions
{
    public Uri? AdresseApi { get; init; }
    public int NombreDePublications { get; init; } = 12;
}

/// <summary>
/// Flux Atom publics de la chaîne. Les vidéos longues et les Shorts ont chacun leur liste de lecture
/// automatique : <c>UULF…</c> (vidéos) et <c>UUSH…</c> (Shorts), suivies de l'identifiant de la chaîne sans « UC ».
/// </summary>
public sealed class FluxYouTubeOptions
{
    public Uri? FluxVideos { get; init; }
    public Uri? FluxCourts { get; init; }
}

public static class EnregistrementContenus
{
    /// <summary>Clé de cache des publications Instagram, à oublier quand un nouveau jeton est saisi.</summary>
    public const string CleInstagram = "contenus:instagram";

    /// <summary>
    /// Enregistre une source par flux configuré, chacune enveloppée dans le décorateur de cache.
    /// Un flux absent de la configuration n'est simplement pas lu.
    /// </summary>
    public static IServiceCollection AjouterContenus(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(ContenusOptions.Section).Get<ContenusOptions>() ?? new ContenusOptions();

        services.AddMemoryCache();
        services.AddHttpClient(SourceYouTube.NomClient, client => client.Timeout = TimeSpan.FromSeconds(8));

        (string Cle, Uri? Adresse)[] flux =
        [
            ("contenus:youtube:videos", options.YouTube.FluxVideos),
            ("contenus:youtube:courts", options.YouTube.FluxCourts),
        ];

        foreach (var (cle, adresse) in flux.Where(f => f.Adresse is not null))
        {
            services.AddSingleton<ISourceDeContenus>(fournisseur => new SourceAvecCache(
                new SourceYouTube(fournisseur.GetRequiredService<IHttpClientFactory>(), adresse!),
                fournisseur.GetRequiredService<IMemoryCache>(),
                fournisseur.GetRequiredService<ILogger<SourceAvecCache>>(),
                cle));
        }

        AjouterInstagram(services, options.Instagram);
        return services;
    }

    private static void AjouterInstagram(IServiceCollection services, InstagramOptions options)
    {
        if (options.AdresseApi is not { } adresseApi)
        {
            services.AddSingleton<IApiInstagram, ApiInstagramNonConfiguree>();
            return;
        }

        services.AddHttpClient(ApiInstagram.NomClient, client => client.Timeout = TimeSpan.FromSeconds(8));
        services.AddSingleton<IApiInstagram>(fournisseur => new ApiInstagram(
            fournisseur.GetRequiredService<IHttpClientFactory>(), adresseApi, options.NombreDePublications));
        services.AddSingleton<ISourceDeContenus>(fournisseur => new SourceAvecCache(
            new SourceInstagram(
                fournisseur.GetRequiredService<IServiceScopeFactory>(),
                fournisseur.GetRequiredService<IApiInstagram>(),
                fournisseur.GetRequiredService<IHorloge>()),
            fournisseur.GetRequiredService<IMemoryCache>(),
            fournisseur.GetRequiredService<ILogger<SourceAvecCache>>(),
            CleInstagram));
    }
}
