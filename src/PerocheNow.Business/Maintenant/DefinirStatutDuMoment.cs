using PerocheNow.Business.Audit;
using PerocheNow.Business.CopiesDeLecture;
using PerocheNow.Business.Mediateur;
using PerocheNow.Business.Ports;
using PerocheNow.Domain.Maintenant;

namespace PerocheNow.Business.Maintenant;

/// <summary>
/// Remplace ce qu'Alexandre fait en ce moment. Crée le statut s'il n'existe pas encore.
/// Renvoie l'identifiant du statut.
/// </summary>
public sealed record DefinirStatutDuMoment(
    string Accroche,
    IReadOnlyList<LigneSaisie> Lignes,
    string? Objectif,
    DateOnly? DateObjectif) : ICommande<Guid>;

/// <summary>Une ligne du moment telle qu'on la saisit : son texte et la rubrique vers laquelle elle mène.</summary>
public sealed record LigneSaisie(string Texte, Rubrique Rubrique = Rubrique.Aucune);

public sealed class DefinirStatutDuMomentGestionnaire(
    IStatutDuMomentRepository statuts,
    IHorloge horloge,
    ICopiesDeLecture copies,
    ActionsApresEnregistrement apresEnregistrement) : IGestionnaire<DefinirStatutDuMoment, Guid>
{
    public async Task<Guid> TraiterAsync(DefinirStatutDuMoment requete, CancellationToken annulation)
    {
        var instant = horloge.Instant;
        var statut = await statuts.ObtenirAsync(annulation);

        if (statut is null)
        {
            statut = StatutDuMoment.Creer(requete.Accroche, instant);
            statuts.Ajouter(statut);
        }
        else
        {
            statut.ModifierAccroche(requete.Accroche, instant);
        }

        statut.RemplacerLignes(requete.Lignes.Select(l => (l.Texte, l.Rubrique)), instant);
        statut.DefinirProchainObjectif(
            string.IsNullOrWhiteSpace(requete.Objectif) ? null : new Objectif(requete.Objectif, requete.DateObjectif),
            instant);

        // L'enregistrement est fait par le pipeline (ComportementUniteDeTravail), avec la trace d'audit.
        // La copie lue par les pages publiques n'est réécrite qu'ensuite, si la base a bien tout gardé.
        var copie = StatutDuMomentDto.Depuis(statut);
        apresEnregistrement.Ajouter(annulationApres => copies.PublierAsync(NomsDesCopies.StatutDuMoment, copie, annulationApres));

        return statut.Id;
    }
}
