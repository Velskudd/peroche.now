using PerocheNow.Business.Mediation;
using PerocheNow.Business.Ports;
using PerocheNow.Domain.Status;

namespace PerocheNow.Business.Status;

/// <summary>
/// Remplace ce qu'Alexandre fait en ce moment. Crée le statut s'il n'existe pas encore.
/// Renvoie l'identifiant du statut.
/// </summary>
public sealed record SetCurrentStatus(
    string Headline,
    IReadOnlyList<LineInput> Lines,
    string? Goal,
    DateOnly? GoalDate) : ICommand<Guid>;

/// <summary>Une ligne du moment telle qu'on la saisit : son texte et la rubrique vers laquelle elle mène.</summary>
public sealed record LineInput(string Text, Section Section = Section.None);

public sealed class SetCurrentStatusHandler(
    ICurrentStatusRepository statuses,
    IClock clock) : IHandler<SetCurrentStatus, Guid>
{
    public async Task<Guid> HandleAsync(SetCurrentStatus request, CancellationToken cancellationToken)
    {
        var now = clock.Now;
        var status = await statuses.GetAsync(cancellationToken);

        if (status is null)
        {
            status = CurrentStatus.Create(request.Headline, now);
            statuses.Add(status);
        }
        else
        {
            status.ChangeHeadline(request.Headline, now);
        }

        status.ReplaceLines(request.Lines.Select(l => (l.Text, l.Section)), now);
        status.SetNextGoal(
            string.IsNullOrWhiteSpace(request.Goal) ? null : new Goal(request.Goal, request.GoalDate),
            now);

        // L'enregistrement est fait par le pipeline (UnitOfWorkBehavior), avec la trace d'audit.
        return status.Id;
    }
}
