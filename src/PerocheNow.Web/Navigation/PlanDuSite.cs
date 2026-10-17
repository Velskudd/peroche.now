using PerocheNow.Domain.Maintenant;

namespace PerocheNow.Web.Navigation;

/// <summary>Une entrée de navigation : son libellé et son adresse.</summary>
public sealed record Destination(string Libelle, string Adresse);

/// <summary>
/// Les pages du site et la rubrique vers laquelle mène chaque ligne du moment.
/// L'accueil résume ; ces pages portent le détail.
/// </summary>
public static class PlanDuSite
{
    public static readonly Destination Maintenant = new("Maintenant", "/");

    /// <summary>Le projet course en cours. Quand NUR24 sera terminé, le suivant prendra sa place ici.</summary>
    public static readonly Destination ProjetEnCours = new("NUR24", "/nur24");

    public static readonly Destination Contenus = new("Contenus", "/contenus");
    public static readonly Destination HorsPiste = new("À côté de la piste", "/contenus");
    public static readonly Destination Partenaires = new("Partenaires", "/partenaires");

    // Tableau nommé : Split('?', '#') appellerait Split(char, int count), « # » devenant le nombre 35.
    private static readonly char[] FinDuChemin = ['?', '#'];

    /// <summary>Le menu principal, dans l'ordre d'affichage.</summary>
    public static IReadOnlyList<Destination> Menu { get; } = [Maintenant, ProjetEnCours, Contenus, Partenaires];

    /// <summary>La page où se trouve le détail d'une ligne du moment, ou <c>null</c> si elle ne mène nulle part.</summary>
    public static Destination? Pour(Rubrique rubrique) => rubrique switch
    {
        Rubrique.ProjetEnCours => ProjetEnCours,
        Rubrique.Contenus => Contenus,
        Rubrique.HorsPiste => HorsPiste,
        _ => null,
    };

    /// <summary>Vrai si l'adresse demandée (relative, sans « / » initial) correspond à cette entrée du menu.</summary>
    public static bool EstCourante(Destination destination, string cheminRelatif)
    {
        var chemin = cheminRelatif.Split(FinDuChemin)[0].Trim('/');
        return string.Equals(destination.Adresse.Trim('/'), chemin, StringComparison.OrdinalIgnoreCase);
    }
}
