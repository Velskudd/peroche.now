using PerocheNow.Business.Audit;
using PerocheNow.Business.CopiesDeLecture;
using PerocheNow.Business.Mediateur;
using PerocheNow.Business.Ports;
using PerocheNow.Domain.Textes;

namespace PerocheNow.Business.Textes;

/// <summary>Un texte tel que saisi dans l'administration. Sans valeur, le texte d'origine reprend sa place.</summary>
public sealed record TexteSaisi(string Cle, string? Valeur);

/// <summary>
/// Enregistre les textes d'une page de l'administration : un texte saisi remplace le texte d'origine,
/// un texte vide le rétablit. Renvoie le nombre de textes modifiés qui restent sur le site.
/// </summary>
public sealed record ModifierTextesDuSite(IReadOnlyList<TexteSaisi> Textes) : ICommande<int>;

public sealed class ModifierTextesDuSiteGestionnaire(
    ITextesDuSiteRepository textes,
    IHorloge horloge,
    ICopiesDeLecture copies,
    ActionsApresEnregistrement apresEnregistrement) : IGestionnaire<ModifierTextesDuSite, int>
{
    public async Task<int> TraiterAsync(ModifierTextesDuSite requete, CancellationToken annulation)
    {
        var maintenant = horloge.Instant;
        var existants = (await textes.ListerAsync(annulation)).ToDictionary(t => t.Cle, StringComparer.Ordinal);

        foreach (var saisi in requete.Textes)
        {
            var cle = saisi.Cle.Trim();
            existants.TryGetValue(cle, out var existant);

            if (string.IsNullOrWhiteSpace(saisi.Valeur))
            {
                if (existant is null)
                    continue;

                textes.Supprimer(existant);
                existants.Remove(cle);
            }
            else if (existant is null)
            {
                var nouveau = TexteDuSite.Creer(cle, saisi.Valeur, maintenant);
                textes.Ajouter(nouveau);
                existants[nouveau.Cle] = nouveau;
            }
            else
            {
                existant.Modifier(saisi.Valeur, maintenant);
            }
        }

        // La copie lue par les pages publiques n'est réécrite qu'une fois la base enregistrée.
        var copie = new TextesPublies(existants.Values.ToDictionary(t => t.Cle, t => t.Valeur, StringComparer.Ordinal));
        apresEnregistrement.Ajouter(annulationApres => copies.PublierAsync(NomsDesCopies.TextesDuSite, copie, annulationApres));

        return existants.Count;
    }
}

/// <summary>Les textes modifiés, repère par repère : ce que publie la copie de lecture.</summary>
public sealed record TextesPublies(IReadOnlyDictionary<string, string> Textes)
{
    public static readonly TextesPublies Aucun = new(new Dictionary<string, string>(StringComparer.Ordinal));
}

/// <summary>Les textes modifiés, lus dans la base : pour l'administration.</summary>
public sealed record ObtenirTextesDuSite : IConsultation<TextesPublies>;

public sealed class ObtenirTextesDuSiteGestionnaire(ITextesDuSiteRepository textes)
    : IGestionnaire<ObtenirTextesDuSite, TextesPublies>
{
    public async Task<TextesPublies> TraiterAsync(ObtenirTextesDuSite requete, CancellationToken annulation) =>
        new((await textes.ListerAsync(annulation)).ToDictionary(t => t.Cle, t => t.Valeur, StringComparer.Ordinal));
}

/// <summary>
/// Les textes modifiés, lus dans leur copie : pour les pages publiques, sans réveiller la base.
/// Si la copie n'existe pas encore, elle est reconstruite une fois depuis la base (même vide).
/// </summary>
public sealed record ObtenirTextesPublies : IConsultation<TextesPublies>;

public sealed class ObtenirTextesPubliesGestionnaire(ICopiesDeLecture copies, ITextesDuSiteRepository textes)
    : IGestionnaire<ObtenirTextesPublies, TextesPublies>
{
    public async Task<TextesPublies> TraiterAsync(ObtenirTextesPublies requete, CancellationToken annulation)
    {
        var publies = await copies.LireAsync<TextesPublies>(NomsDesCopies.TextesDuSite, annulation);
        if (publies is not null)
            return publies;

        var reconstruits = new TextesPublies(
            (await textes.ListerAsync(annulation)).ToDictionary(t => t.Cle, t => t.Valeur, StringComparer.Ordinal));
        await copies.PublierAsync(NomsDesCopies.TextesDuSite, reconstruits, annulation);
        return reconstruits;
    }
}
