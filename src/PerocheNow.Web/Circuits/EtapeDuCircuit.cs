namespace PerocheNow.Web.Circuits;

/// <summary>Où en est une étape : déjà franchie, en cours, à venir, ou l'objectif final.</summary>
public enum EtatEtape
{
    Franchie,
    EnCours,
    AVenir,
    Objectif,
}

/// <summary>
/// Une étape posée sur un secteur du circuit : son repère sur le tracé (une année, un numéro),
/// ce que dit sa bulle, et éventuellement une photo (son adresse, déjà résolue) et sa description.
/// </summary>
public sealed record EtapeDuCircuit(
    string Repere,
    string Titre,
    string Texte,
    EtatEtape Etat,
    string? Photo = null,
    string? TextePhoto = null);
