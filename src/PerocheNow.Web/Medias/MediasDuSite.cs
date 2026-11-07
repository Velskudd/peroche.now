using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Memory;
using PerocheNow.Business.Medias;
using PerocheNow.Business.Mediateur;
using PerocheNow.Web.Textes;

namespace PerocheNow.Web.Medias;

/// <summary>
/// Les photos et vidéos du site pour la requête en cours : le fichier envoyé depuis l'administration s'il existe,
/// sinon le fichier d'origine. Chargé avant le rendu, comme les textes, depuis la copie de lecture.
/// </summary>
public sealed class MediasDuSite(IMediateur mediateur, IMemoryCache cache, TextesDuSite textes, ILogger<MediasDuSite> journal)
{
    public const string Prefixe = "/medias/";

    private const string CleCache = "medias:publies";
    private static readonly TimeSpan DureeCache = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AttenteMaximale = TimeSpan.FromSeconds(4);

    private IReadOnlyDictionary<string, MediaPublie> _envoyes = MediasPublies.Aucun.Medias;

    /// <summary>
    /// L'adresse à mettre dans la page, ou <c>null</c> si l'emplacement n'a ni fichier envoyé ni fichier d'origine.
    /// Le fichier d'origine passe par <paramref name="assets"/> pour garder son empreinte de cache.
    /// </summary>
    public string? Adresse(EmplacementMedia emplacement, ResourceAssetCollection assets)
    {
        if (_envoyes.TryGetValue(emplacement.Cle, out var envoye))
            return Prefixe + envoye.Fichier;
        return emplacement.ParDefaut is null ? null : assets[emplacement.ParDefaut];
    }

    /// <summary>La description d'une photo pour les lecteurs d'écran (vide pour une image décorative).</summary>
    public string Description(EmplacementMedia emplacement) =>
        emplacement.Description is null ? string.Empty : textes[emplacement.Description];

    public bool EstEnvoye(EmplacementMedia emplacement) => _envoyes.ContainsKey(emplacement.Cle);

    /// <summary>Le fichier envoyé pour cet emplacement, ou <c>null</c> s'il garde son fichier d'origine.</summary>
    public MediaPublie? Envoye(EmplacementMedia emplacement) => _envoyes.GetValueOrDefault(emplacement.Cle);

    public async Task ChargerAsync(CancellationToken annulation)
    {
        if (cache.TryGetValue(CleCache, out MediasPublies? enCache) && enCache is not null)
        {
            _envoyes = enCache.Medias;
            return;
        }

        using var delai = CancellationTokenSource.CreateLinkedTokenSource(annulation);
        delai.CancelAfter(AttenteMaximale);

        try
        {
            var publies = await mediateur.EnvoyerAsync(new ObtenirMediasPublies(), delai.Token);
            cache.Set(CleCache, publies, DureeCache);
            _envoyes = publies.Medias;
        }
        catch (Exception erreur) when (!annulation.IsCancellationRequested)
        {
            journal.LogWarning(erreur, "Médias envoyés indisponibles, affichage des fichiers d'origine.");
        }
    }

    public void Oublier() => cache.Remove(CleCache);
}
