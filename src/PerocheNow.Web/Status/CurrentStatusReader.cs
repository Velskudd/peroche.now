using Microsoft.Extensions.Caching.Memory;
using PerocheNow.Business.Mediation;
using PerocheNow.Business.Ports;
using PerocheNow.Business.Status;

namespace PerocheNow.Web.Status;

/// <summary>
/// Lit le statut du moment pour l'accueil.
/// <list type="bullet">
/// <item>Le résultat est gardé quelques minutes en mémoire : la base serverless peut rester en pause.</item>
/// <item>Si la base met trop longtemps à se réveiller ou ne répond pas, l'accueil affiche le statut de secours
/// au lieu d'une erreur. Le secours n'est pas mis en cache, pour retenter la base à la visite suivante.</item>
/// </list>
/// </summary>
public sealed class CurrentStatusReader(
    IMediator mediator,
    IMemoryCache cache,
    IClock clock,
    ILogger<CurrentStatusReader> logger)
{
    private const string CacheKey = "now:status";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(12);

    public async Task<CurrentStatusDto> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out CurrentStatusDto? cached) && cached is not null)
            return cached;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(MaxWait);

        try
        {
            var status = await mediator.SendAsync(new GetCurrentStatus(), timeout.Token);
            if (status is null)
                return InitialStatus.AsFallbackStatus(clock.Now);

            cache.Set(CacheKey, status, CacheDuration);
            return status;
        }
        catch (Exception error) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(error, "Statut du moment indisponible, affichage du statut de secours.");
            return InitialStatus.AsFallbackStatus(clock.Now);
        }
    }
}
