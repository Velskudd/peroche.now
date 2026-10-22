using PerocheNow.Business.Mediateur;
using PerocheNow.Business.Ports;

namespace PerocheNow.Business.Contenus;

/// <summary>Les derniers contenus de toutes les plateformes branchées, du plus récent au plus ancien.</summary>
public sealed record ObtenirDerniersContenus : IConsultation<IReadOnlyList<Contenu>>;

/// <summary>
/// Interroge toutes les sources en parallèle et fusionne leurs contenus.
/// Chaque source est une stratégie (<see cref="ISourceDeContenus"/>) : ajouter une plateforme
/// revient à enregistrer une source de plus, sans toucher à ce gestionnaire.
/// </summary>
public sealed class ObtenirDerniersContenusGestionnaire(IEnumerable<ISourceDeContenus> sources)
    : IGestionnaire<ObtenirDerniersContenus, IReadOnlyList<Contenu>>
{
    public async Task<IReadOnlyList<Contenu>> TraiterAsync(ObtenirDerniersContenus requete, CancellationToken annulation)
    {
        var parSource = await Task.WhenAll(sources.Select(source => source.ObtenirAsync(annulation)));

        return parSource
            .SelectMany(contenus => contenus)
            .OrderByDescending(contenu => contenu.PublieLe)
            .ToList();
    }
}
