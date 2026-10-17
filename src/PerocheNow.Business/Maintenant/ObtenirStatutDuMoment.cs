using PerocheNow.Business.Mediateur;
using PerocheNow.Business.Ports;
using PerocheNow.Domain.Maintenant;

namespace PerocheNow.Business.Maintenant;

/// <summary>Ce que l'accueil affiche en premier : ce qu'Alexandre fait en ce moment.</summary>
public sealed record ObtenirStatutDuMoment : IConsultation<StatutDuMomentDto?>;

public sealed record StatutDuMomentDto(
    string Accroche,
    IReadOnlyList<LigneDuMomentDto> Lignes,
    ObjectifDto? ProchainObjectif,
    DateTimeOffset MisAJourLe);

public sealed record LigneDuMomentDto(string Texte, Rubrique Rubrique);

public sealed record ObjectifDto(string Libelle, DateOnly? DateCible);

public sealed class ObtenirStatutDuMomentGestionnaire(IStatutDuMomentRepository statuts)
    : IGestionnaire<ObtenirStatutDuMoment, StatutDuMomentDto?>
{
    public async Task<StatutDuMomentDto?> TraiterAsync(ObtenirStatutDuMoment requete, CancellationToken annulation)
    {
        var statut = await statuts.ObtenirAsync(annulation);
        if (statut is null)
            return null;

        return new StatutDuMomentDto(
            statut.Accroche,
            statut.Lignes.OrderBy(l => l.Ordre).Select(l => new LigneDuMomentDto(l.Texte, l.Rubrique)).ToList(),
            statut.ProchainObjectif is { } objectif ? new ObjectifDto(objectif.Libelle, objectif.DateCible) : null,
            statut.MisAJourLe);
    }
}
