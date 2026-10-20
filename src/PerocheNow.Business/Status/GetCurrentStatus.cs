using PerocheNow.Business.Mediation;
using PerocheNow.Business.Ports;
using PerocheNow.Domain.Status;

namespace PerocheNow.Business.Status;

/// <summary>Ce que l'accueil affiche en premier : ce qu'Alexandre fait en ce moment.</summary>
public sealed record GetCurrentStatus : IQuery<CurrentStatusDto?>;

public sealed record CurrentStatusDto(
    string Headline,
    IReadOnlyList<StatusLineDto> Lines,
    GoalDto? NextGoal,
    DateTimeOffset UpdatedAt);

public sealed record StatusLineDto(string Text, Section Section);

public sealed record GoalDto(string Label, DateOnly? TargetDate);

public sealed class GetCurrentStatusHandler(ICurrentStatusRepository statuses)
    : IHandler<GetCurrentStatus, CurrentStatusDto?>
{
    public async Task<CurrentStatusDto?> HandleAsync(GetCurrentStatus request, CancellationToken cancellationToken)
    {
        var status = await statuses.GetAsync(cancellationToken);
        if (status is null)
            return null;

        return new CurrentStatusDto(
            status.Headline,
            status.Lines.OrderBy(l => l.Position).Select(l => new StatusLineDto(l.Text, l.Section)).ToList(),
            status.NextGoal is { } goal ? new GoalDto(goal.Label, goal.TargetDate) : null,
            status.UpdatedAt);
    }
}
