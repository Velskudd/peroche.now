using Microsoft.Extensions.Caching.Memory;
using PerocheNow.Business.Maintenant;
using PerocheNow.Business.Mediateur;
using PerocheNow.Business.Ports;

namespace PerocheNow.Web.Maintenant;

/// <summary>
/// Lit le statut du moment pour l'accueil, dans sa copie de lecture : la base reste en pause.
/// <list type="bullet">
/// <item>Le résultat est gardé quelques minutes en mémoire, pour ne pas relire la copie à chaque page.</item>
/// <item>Si la copie manque et que la base met trop longtemps à se réveiller ou ne répond pas, l'accueil affiche le statut de secours
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

    /// <summary>Oublie le statut gardé en mémoire : la prochaine lecture repart de la base (après une modification).</summary>
    public void Oublier() => cache.Remove(CleCache);

    public Task<StatutDuMomentDto> LireAsync(CancellationToken annulation = default) =>
        LireAsync(AttenteMaximale, annulation);

    /// <summary>
    /// Même lecture, avec une attente plus courte pour les pages qui doivent s'afficher tout de suite
    /// (la page de liens, ouverte depuis une bio) : au-delà, statut de secours.
    /// </summary>
    public async Task<StatutDuMomentDto> LireAsync(TimeSpan attenteMaximale, CancellationToken annulation = default)
    {
        if (cache.TryGetValue(CleCache, out StatutDuMomentDto? enCache) && enCache is not null)
            return enCache;

        using var delai = CancellationTokenSource.CreateLinkedTokenSource(annulation);
        delai.CancelAfter(attenteMaximale);

        try
        {
            var statut = await mediateur.EnvoyerAsync(new ObtenirStatutPublie(), delai.Token);
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
