namespace PerocheNow.Business.Maintenant;

/// <summary>
/// Le premier statut déposé dans une base vide. Il sert aussi de statut de secours
/// quand la base ne répond pas (base en pause, quota gratuit épuisé…), pour que l'accueil reste lisible.
/// </summary>
public static class StatutInitial
{
    public static DefinirStatutDuMoment Commande { get; } = new(
        Accroche: "Je prépare ma 1ère course sur la Nordschleife",
        Lignes:
        [
            "Sur iRacing, je roule en NEC en Porsche Cup pour enchaîner les tours de Nordschleife.",
            "Sur Twitch, au moins deux lives par semaine, redécoupés en clips chaque jour.",
            "Hors piste, je termine Dragon Quest VII Reimagined.",
        ],
        Objectif: "2e partie de saison : RCN 2027",
        DateObjectif: new DateOnly(2027, 9, 1));

    public static StatutDuMomentDto CommeStatutDeSecours(DateTimeOffset maintenant) => new(
        Commande.Accroche,
        Commande.Lignes,
        Commande.Objectif is { } libelle ? new ObjectifDto(libelle, Commande.DateObjectif) : null,
        maintenant);
}
