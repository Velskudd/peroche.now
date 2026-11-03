using System.Globalization;
using PerocheNow.Business.Ports;
using PerocheNow.Web.Textes;

namespace PerocheNow.Web.Circuits;

/// <summary>Les étapes posées sur les circuits : la route jusqu'aux 24 Heures, et le parcours déjà fait.</summary>
public static class EtapesDuProjet
{
    private sealed record Saison(int Annee, DefinitionTexte Titre, DefinitionTexte Texte, string? Photo = null, string? TextePhoto = null);

    // Les années restent dans le code : elles décident de ce qui est franchi. Les textes se modifient dans l'administration.
    private static readonly Saison[] Saisons =
    [
        new(2026, TextesNur24.Route2026Titre, TextesNur24.Route2026Texte, "images/volant-regard.webp", "Alexandre au volant, en combinaison de course"),
        new(2027, TextesNur24.Route2027Titre, TextesNur24.Route2027Texte),
        new(2028, TextesNur24.Route2028Titre, TextesNur24.Route2028Texte),
        new(2029, TextesNur24.Route2029Titre, TextesNur24.Route2029Texte),
    ];

    /// <summary>Une saison par secteur ; l'année en cours est « en cours », la dernière est l'objectif.</summary>
    public static IReadOnlyList<EtapeDuCircuit> Route(TextesDuSite textes, IHorloge horloge)
    {
        var annee = horloge.Instant.Year;
        var derniere = Saisons[^1].Annee;
        return Saisons
            .Select(saison => new EtapeDuCircuit(
                saison.Annee.ToString(CultureInfo.InvariantCulture),
                textes[saison.Titre],
                textes[saison.Texte],
                Etat(saison.Annee, annee, derniere),
                saison.Photo,
                saison.TextePhoto))
            .ToList();
    }

    private static EtatEtape Etat(int saison, int anneeEnCours, int derniere)
    {
        if (saison < anneeEnCours)
            return EtatEtape.Franchie;
        if (saison == anneeEnCours)
            return EtatEtape.EnCours;
        return saison == derniere ? EtatEtape.Objectif : EtatEtape.AVenir;
    }

    /// <summary>Une étape du parcours par secteur ; la dernière est l'étape actuelle.</summary>
    public static IReadOnlyList<EtapeDuCircuit> Parcours(TextesDuSite textes)
    {
        var etapes = textes.Elements(TextesNur24.ParcoursEtapes);
        var details = textes.Elements(TextesNur24.ParcoursDetails);
        return etapes
            .Select((titre, i) => new EtapeDuCircuit(
                (i + 1).ToString(CultureInfo.InvariantCulture),
                titre,
                i < details.Count ? details[i] : string.Empty,
                i == etapes.Count - 1 ? EtatEtape.EnCours : EtatEtape.Franchie))
            .ToList();
    }
}
