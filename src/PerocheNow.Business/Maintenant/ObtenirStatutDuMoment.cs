using PerocheNow.Business.CopiesDeLecture;
using PerocheNow.Business.Mediateur;
using PerocheNow.Business.Ports;
using PerocheNow.Domain.Maintenant;

namespace PerocheNow.Business.Maintenant;

/// <summary>
/// Ce qu'Alexandre fait en ce moment, lu dans la base : pour l'administration, qui modifie la référence.
/// Les pages publiques lisent la copie (<see cref="ObtenirStatutPublie"/>).
/// </summary>
public sealed record ObtenirStatutDuMoment : IConsultation<StatutDuMomentDto?>;

public sealed record StatutDuMomentDto(
    string Accroche,
    IReadOnlyList<LigneDuMomentDto> Lignes,
    ObjectifDto? ProchainObjectif,
    DateTimeOffset MisAJourLe)
{
    public static StatutDuMomentDto Depuis(StatutDuMoment statut) => new(
        statut.Accroche,
        statut.Lignes.OrderBy(l => l.Ordre).Select(l => new LigneDuMomentDto(l.Texte, l.Rubrique)).ToList(),
        statut.ProchainObjectif is { } objectif ? new ObjectifDto(objectif.Libelle, objectif.DateCible) : null,
        statut.MisAJourLe);
}

public sealed record LigneDuMomentDto(string Texte, Rubrique Rubrique);

public sealed record ObjectifDto(string Libelle, DateOnly? DateCible);

public sealed class ObtenirStatutDuMomentGestionnaire(IStatutDuMomentRepository statuts)
    : IGestionnaire<ObtenirStatutDuMoment, StatutDuMomentDto?>
{
    public async Task<StatutDuMomentDto?> TraiterAsync(ObtenirStatutDuMoment requete, CancellationToken annulation)
    {
        var statut = await statuts.ObtenirAsync(annulation);
        return statut is null ? null : StatutDuMomentDto.Depuis(statut);
    }
}

/// <summary>
/// Ce que l'accueil affiche en premier, lu dans la copie de lecture : la base n'est pas réveillée.
/// Si la copie n'existe pas encore (premier démarrage), elle est reconstruite une fois depuis la base.
/// </summary>
public sealed record ObtenirStatutPublie : IConsultation<StatutDuMomentDto?>;

public sealed class ObtenirStatutPublieGestionnaire(ICopiesDeLecture copies, IStatutDuMomentRepository statuts)
    : IGestionnaire<ObtenirStatutPublie, StatutDuMomentDto?>
{
    public async Task<StatutDuMomentDto?> TraiterAsync(ObtenirStatutPublie requete, CancellationToken annulation)
    {
        var publie = await copies.LireAsync<StatutDuMomentDto>(NomsDesCopies.StatutDuMoment, annulation);
        if (publie is not null)
            return publie;

        var statut = await statuts.ObtenirAsync(annulation);
        if (statut is null)
            return null;

        var copie = StatutDuMomentDto.Depuis(statut);
        await copies.PublierAsync(NomsDesCopies.StatutDuMoment, copie, annulation);
        return copie;
    }
}
