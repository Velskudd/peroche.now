using Microsoft.Extensions.Caching.Memory;
using PerocheNow.Business.Mediateur;
using PerocheNow.Business.Textes;

namespace PerocheNow.Web.Textes;

/// <summary>
/// Les textes du site pour la requête en cours : le texte modifié dans l'administration s'il existe,
/// sinon le texte d'origine. Chargé une fois avant le rendu (voir <see cref="ChargementDesTextes"/>),
/// puis lu sans attente par les composants.
/// <list type="bullet">
/// <item>Les textes modifiés viennent de leur copie de lecture : la base reste en pause.</item>
/// <item>Ils sont gardés quelques minutes en mémoire, et oubliés dès qu'on enregistre dans l'administration.</item>
/// <item>Si la copie ne répond pas à temps, la page s'affiche avec les textes d'origine plutôt que d'attendre.</item>
/// </list>
/// </summary>
public sealed class TextesDuSite(IMediateur mediateur, IMemoryCache cache, ILogger<TextesDuSite> journal)
{
    private const string CleCache = "textes:publies";
    private static readonly TimeSpan DureeCache = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AttenteMaximale = TimeSpan.FromSeconds(4);

    private IReadOnlyDictionary<string, string> _modifies = TextesPublies.Aucun.Textes;

    /// <summary>Le texte à afficher.</summary>
    public string this[DefinitionTexte texte] =>
        _modifies.TryGetValue(texte.Cle, out var modifie) ? modifie : texte.ParDefaut;

    /// <summary>Vrai si le texte a été modifié dans l'administration.</summary>
    public bool EstModifie(DefinitionTexte texte) => _modifies.ContainsKey(texte.Cle);

    /// <summary>Les éléments d'un texte en liste, un par ligne non vide.</summary>
    public IReadOnlyList<string> Elements(DefinitionTexte texte) =>
        this[texte].Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>L'élément d'une liste à cette position, ou celui d'origine si la liste modifiée est plus courte.</summary>
    public string Element(DefinitionTexte texte, int position)
    {
        var elements = Elements(texte);
        if (position < elements.Count)
            return elements[position];

        var origine = texte.ParDefaut.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return position < origine.Length ? origine[position] : string.Empty;
    }

    public async Task ChargerAsync(CancellationToken annulation)
    {
        if (cache.TryGetValue(CleCache, out TextesPublies? enCache) && enCache is not null)
        {
            _modifies = enCache.Textes;
            return;
        }

        using var delai = CancellationTokenSource.CreateLinkedTokenSource(annulation);
        delai.CancelAfter(AttenteMaximale);

        try
        {
            var publies = await mediateur.EnvoyerAsync(new ObtenirTextesPublies(), delai.Token);
            cache.Set(CleCache, publies, DureeCache);
            _modifies = publies.Textes;
        }
        catch (Exception erreur) when (!annulation.IsCancellationRequested)
        {
            // Pas de mise en cache : la visite suivante retentera.
            journal.LogWarning(erreur, "Textes modifiés indisponibles, affichage des textes d'origine.");
        }
    }

    /// <summary>Oublie les textes gardés en mémoire : la page suivante relit la copie (après un enregistrement).</summary>
    public void Oublier() => cache.Remove(CleCache);
}

/// <summary>
/// Charge les textes et les médias avant le rendu de chaque page
/// (pas pour les fichiers, les médias eux-mêmes ni la sonde de santé).
/// </summary>
public static class ChargementDesTextes
{
    public static IApplicationBuilder UseTextesDuSite(this IApplicationBuilder app) =>
        app.Use(async (contexte, suite) =>
        {
            var chemin = contexte.Request.Path;
            if (!Path.HasExtension(chemin) && !chemin.StartsWithSegments("/sante", StringComparison.OrdinalIgnoreCase))
            {
                var services = contexte.RequestServices;
                await services.GetRequiredService<TextesDuSite>().ChargerAsync(contexte.RequestAborted);
                await services.GetRequiredService<PerocheNow.Web.Medias.MediasDuSite>().ChargerAsync(contexte.RequestAborted);
            }

            await suite(contexte);
        });
}
