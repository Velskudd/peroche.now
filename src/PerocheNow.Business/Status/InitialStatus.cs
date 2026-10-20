using PerocheNow.Domain.Status;

namespace PerocheNow.Business.Status;

/// <summary>
/// Le premier statut déposé dans une base vide. Il sert aussi de statut de secours
/// quand la base ne répond pas (base en pause, quota gratuit épuisé…), pour que l'accueil reste lisible.
/// </summary>
public static class InitialStatus
{
    public static SetCurrentStatus Command { get; } = new(
        Headline: "Je prépare ma 1re course sur la Nordschleife",
        Lines:
        [
            new("Sur iRacing, je roule en NEC en Porsche Cup pour enchaîner les tours de Nordschleife.", Section.CurrentProject),
            new("Sur Twitch, au moins deux lives par semaine, redécoupés en clips chaque jour.", Section.Content),
            new("Hors piste, je termine Dragon Quest VII Reimagined.", Section.OffTrack),
        ],
        Goal: "2e partie de saison : RCN 2027",
        GoalDate: new DateOnly(2027, 9, 1));

    public static CurrentStatusDto AsFallbackStatus(DateTimeOffset now) => new(
        Command.Headline,
        Command.Lines.Select(l => new StatusLineDto(l.Text, l.Section)).ToList(),
        Command.Goal is { } label ? new GoalDto(label, Command.GoalDate) : null,
        now);
}
