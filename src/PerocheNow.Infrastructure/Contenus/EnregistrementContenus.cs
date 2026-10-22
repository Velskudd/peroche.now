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

        return services;
    }
}
