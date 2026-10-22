namespace PerocheNow.Business.Contenus;

/// <summary>La plateforme d'où vient un contenu.</summary>
public enum Plateforme
{
    YouTube,
    Instagram,
}

/// <summary>La forme d'un contenu, qui décide où et comment le site l'affiche.</summary>
public enum FormatDeContenu
{
    /// <summary>Vidéo longue en 16:9 : vlog, onboard, rediffusion de course.</summary>
    Video,

    /// <summary>Format court vertical : Short YouTube ou Reel Instagram.</summary>
    Court,

    /// <summary>Photo ou carrousel Instagram.</summary>
    Publication,
}

/// <summary>Un contenu publié sur une plateforme, tel que le site l'affiche.</summary>
public sealed record Contenu(
    Plateforme Plateforme,
    FormatDeContenu Format,
    string Titre,
    Uri Lien,
    Uri? Miniature,
    DateTimeOffset PublieLe);
