using PerocheNow.Domain.Status;

namespace PerocheNow.Web.Navigation;

/// <summary>Une entrée de navigation : son libellé et son adresse.</summary>
public sealed record Destination(string Label, string Url);

/// <summary>
/// Les pages du site et la rubrique vers laquelle mène chaque ligne du moment.
/// L'accueil résume ; ces pages portent le détail.
/// </summary>
public static class SiteMap
{
    public static readonly Destination Home = new("Maintenant", "/");

    /// <summary>Le projet course en cours. Quand NUR24 sera terminé, le suivant prendra sa place ici.</summary>
    public static readonly Destination CurrentProject = new("NUR24", "/nur24");

    public static readonly Destination Content = new("Contenus", "/contenus");
    public static readonly Destination OffTrack = new("À côté de la piste", "/contenus");
    public static readonly Destination Partners = new("Partenaires", "/partenaires");

    // Tableau nommé : Split('?', '#') appellerait Split(char, int count), « # » devenant le nombre 35.
    private static readonly char[] PathTerminators = ['?', '#'];

    /// <summary>Le menu principal, dans l'ordre d'affichage.</summary>
    public static IReadOnlyList<Destination> Menu { get; } = [Home, CurrentProject, Content, Partners];

    /// <summary>La page où se trouve le détail d'une ligne du moment, ou <c>null</c> si elle ne mène nulle part.</summary>
    public static Destination? For(Section section) => section switch
    {
        Section.CurrentProject => CurrentProject,
        Section.Content => Content,
        Section.OffTrack => OffTrack,
        _ => null,
    };

    /// <summary>Vrai si l'adresse demandée (relative, sans « / » initial) correspond à cette entrée du menu.</summary>
    public static bool IsCurrent(Destination destination, string relativePath)
    {
        var path = relativePath.Split(PathTerminators)[0].Trim('/');
        return string.Equals(destination.Url.Trim('/'), path, StringComparison.OrdinalIgnoreCase);
    }
}
