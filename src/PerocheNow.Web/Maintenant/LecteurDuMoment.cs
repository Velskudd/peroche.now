using Microsoft.Extensions.Caching.Memory;
using PerocheNow.Business.Maintenant;
using PerocheNow.Business.Mediateur;
using PerocheNow.Business.Ports;

namespace PerocheNow.Web.Maintenant;

/// <summary>
/// Lit le statut du moment pour l'accueil.
/// <list type="bullet">
/// <item>Le résultat est gardé quelques minutes en mémoire : la base serverless peut rester en pause.</item>
/// <item>Si la base met trop longtemps à se réveiller ou ne répond pas, l'accueil affiche le statut de secours
/// au lieu d'une erreur. Le secours n'est pas mis en cache, pour retenter la base à la visite suivante.</item>
/// </list>
/// </summary>
public sealed class LecteurDuMoment(
    IMediateur mediateur,
    IMemoryCache cache,
    IHorloge horloge,
    ILogger<LecteurDuMoment> journal)
{
    private const string CleCache = "maintenant:statut";
    private static readonly TimeSpan DureeCache = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AttenteMaximale = TimeSpan.FromSeconds(12);

    public async Task<StatutDuMomentDto> LireAsync(CancellationToken annulation = default)
    {
        if (cache.TryGetValue(CleCache, out StatutDuMomentDto? enCache) && enCache is not null)
            return enCache;

        using var delai = CancellationTokenSource.CreateLinkedTokenSource(annulation);
        delai.CancelAfter(AttenteMaximale);

        try
        {
            var statut = await mediateur.EnvoyerAsync(new ObtenirStatutDuMoment(), delai.Token);
            if (statut is null)
                return StatutInitial.CommeStatutDeSecours(horloge.Instant);

            cache.Set(CleCache, statut, DureeCache);
            return statut;
        }
        catch (Exception erreur) when (!annulation.IsCancellationRequested)
        {
            journal.LogWarning(erreur, "Statut du moment indisponible, affichage du statut de secours.");
            return StatutInitial.CommeStatutDeSecours(horloge.Instant);
        }
    }
}
