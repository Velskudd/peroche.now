namespace PerocheNow.Domain.Status;

/// <summary>
/// La partie du site vers laquelle mène une ligne du moment.
/// L'accueil résume ; chaque ligne ouvre la rubrique où se trouve le détail.
/// </summary>
public enum Section
{
    /// <summary>La ligne ne mène nulle part.</summary>
    None = 0,

    /// <summary>Le projet course en cours (aujourd'hui NUR24).</summary>
    CurrentProject = 1,

    /// <summary>Les vidéos, Reels et lives.</summary>
    Content = 2,

    /// <summary>Les projets hors course : gaming, cartes, sport…</summary>
    OffTrack = 3,
}
