using Microsoft.Extensions.Caching.Memory;
using PerocheNow.Business.Direct;
using PerocheNow.Business.Mediateur;
using PerocheNow.Business.Ports;

namespace PerocheNow.Web.Direct;

/// <summary>
/// Dit si un direct Twitch est en cours, pour la pastille « En direct ».
/// <list type="bullet">
/// <item>La réponse est gardée une minute : Twitch n'est pas interrogé à chaque page vue.</item>
/// <item>Si Twitch tarde ou ne répond pas, la pastille est simplement masquée, sans retarder la page.</item>
/// </list>
/// </summary>
public sealed class LecteurDuDirect(IMediateur mediateur, IMemoryCache cache, ILogger<LecteurDuDirect> journal)
{
    private const string CleCache = "direct:twitch";
    private static readonly TimeSpan DureeCache = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan AttenteMaximale = TimeSpan.FromSeconds(2);

    public async Task<DirectEnCours?> LireAsync(CancellationToken annulation = default)
    {
        if (cache.TryGetValue(CleCache, out EtatDuDirect? enCache) && enCache is not null)
            return enCache.Direct;

        using var delai = CancellationTokenSource.CreateLinkedTokenSource(annulation);
        delai.CancelAfter(AttenteMaximale);

        DirectEnCours? direct;
        try
        {
            direct = await mediateur.EnvoyerAsync(new ObtenirDirect(), delai.Token);
        }
        catch (Exception erreur) when (!annulation.IsCancellationRequested)
        {
            journal.LogWarning(erreur, "État du direct Twitch indisponible, pastille masquée.");
            direct = null;
        }

        // Même un échec est gardé une minute, pour ne pas relancer Twitch à chaque page vue.
        cache.Set(CleCache, new EtatDuDirect(direct), DureeCache);
        return direct;
    }

    private sealed record EtatDuDirect(DirectEnCours? Direct);
}
