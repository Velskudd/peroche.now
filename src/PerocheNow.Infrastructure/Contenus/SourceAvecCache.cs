using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PerocheNow.Business.Contenus;
using PerocheNow.Business.Ports;

namespace PerocheNow.Infrastructure.Contenus;

/// <summary>
/// Décorateur d'une <see cref="ISourceDeContenus"/> : il garde les contenus en mémoire un quart d'heure
/// et, si la plateforme ne répond pas, renvoie les derniers contenus connus au lieu d'une erreur.
/// <para>
/// La plateforme est interrogée indépendamment de la requête du visiteur : si la page n'attend pas
/// la réponse, celle-ci sert quand même à remplir le cache pour la visite suivante.
/// </para>
/// </summary>
public sealed class SourceAvecCache(
    ISourceDeContenus source,
    IMemoryCache cache,
    ILogger<SourceAvecCache> journal,
    string cle) : ISourceDeContenus
{
    private static readonly TimeSpan DureeSucces = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DureeEchec = TimeSpan.FromMinutes(2);

    private volatile IReadOnlyList<Contenu> _derniersConnus = [];

    public Task<IReadOnlyList<Contenu>> ObtenirAsync(CancellationToken annulation)
    {
        if (cache.TryGetValue(cle, out IReadOnlyList<Contenu>? enCache) && enCache is not null)
            return Task.FromResult(enCache);

        return RafraichirAsync().WaitAsync(annulation);
    }

    private async Task<IReadOnlyList<Contenu>> RafraichirAsync()
    {
        try
        {
            var contenus = await source.ObtenirAsync(CancellationToken.None);
            _derniersConnus = contenus;
            cache.Set(cle, contenus, DureeSucces);
            return contenus;
        }
        catch (Exception erreur)
        {
            journal.LogWarning(erreur, "Contenus « {Cle} » indisponibles : derniers contenus connus conservés.", cle);
            var derniers = _derniersConnus;
            cache.Set(cle, derniers, DureeEchec);
            return derniers;
        }
    }
}
