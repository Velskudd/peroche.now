namespace PerocheNow.Domain.Maintenant;

/// <summary>
/// La partie du site vers laquelle mène une ligne du moment.
/// L'accueil résume ; chaque ligne ouvre la rubrique où se trouve le détail.
/// </summary>
public enum Rubrique
{
    /// <summary>La ligne ne mène nulle part.</summary>
    Aucune = 0,

    /// <summary>Le projet course en cours (aujourd'hui NUR24).</summary>
    ProjetEnCours = 1,

    /// <summary>Les vidéos, Reels et lives.</summary>
    Contenus = 2,

    /// <summary>Les projets hors course : gaming, cartes, sport…</summary>
    HorsPiste = 3,
}
