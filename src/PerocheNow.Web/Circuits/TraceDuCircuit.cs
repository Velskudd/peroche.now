using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace PerocheNow.Web.Circuits;

/// <summary>Un point dans le repère du tracé.</summary>
public readonly record struct PointDuCircuit(double X, double Y);

/// <summary>
/// Un secteur de la boucle nord : son tracé SVG, son point de départ, son milieu, où poser son repère
/// et comment l'aligner (<c>start</c>, <c>end</c> ou <c>middle</c>) pour qu'il ne mange pas le tracé.
/// </summary>
public sealed record SecteurDuCircuit(string Chemin, PointDuCircuit Debut, PointDuCircuit Milieu, PointDuCircuit Repere, string Ancrage);

/// <summary>
/// La Nordschleife découpée en secteurs de même longueur, dans le sens de la course :
/// le premier part du raccordement au circuit Grand Prix, le dernier finit par Döttinger Höhe.
/// </summary>
public static class TraceDuCircuit
{
    public const double OrigineX = -10;
    public const double OrigineY = -10;
    public const double Largeur = 1020;
    public const double Hauteur = 1064;
    public const string ViewBox = "-10 -10 1020 1064";

    /// <summary>Distance entre le tracé et le repère (année ou numéro) d'un secteur, vers l'extérieur.</summary>
    private const double EcartDuRepere = 34;

    private static readonly PointDuCircuit[] Points = Lire(DonneesNordschleife.Boucle);
    private static readonly PointDuCircuit Centre = new(Points.Average(p => p.X), Points.Average(p => p.Y));
    private static readonly ConcurrentDictionary<int, IReadOnlyList<SecteurDuCircuit>> Decoupages = new();

    public static string Contexte => DonneesNordschleife.Contexte;

    public static IReadOnlyList<SecteurDuCircuit> Secteurs(int nombre)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nombre, 1);
        return Decoupages.GetOrAdd(nombre, Decouper);
    }

    /// <summary>La position d'un point en pourcentage de la largeur et de la hauteur du dessin.</summary>
    public static (double Gauche, double Haut) EnPourcentage(PointDuCircuit point) =>
        ((point.X - OrigineX) / Largeur * 100, (point.Y - OrigineY) / Hauteur * 100);

    public static string Nombre(double valeur) => valeur.ToString("0.#", CultureInfo.InvariantCulture);

    private static List<SecteurDuCircuit> Decouper(int nombre)
    {
        // La boucle est fermée : on revient au premier point.
        var boucle = Points.Append(Points[0]).ToArray();
        var cumuls = new double[boucle.Length];
        for (var i = 1; i < boucle.Length; i++)
            cumuls[i] = cumuls[i - 1] + Distance(boucle[i - 1], boucle[i]);

        var total = cumuls[^1];
        var secteurs = new List<SecteurDuCircuit>(nombre);
        for (var s = 0; s < nombre; s++)
        {
            var debut = total * s / nombre;
            var fin = total * (s + 1) / nombre;

            var points = new List<PointDuCircuit> { PointA(boucle, cumuls, debut) };
            points.AddRange(boucle.Where((_, i) => cumuls[i] > debut && cumuls[i] < fin));
            points.Add(PointA(boucle, cumuls, fin));

            var milieu = PointA(boucle, cumuls, (debut + fin) / 2);
            var repere = VersLExterieur(milieu);
            secteurs.Add(new SecteurDuCircuit(Chemin(points), points[0], milieu, repere, Ancrage(milieu, repere)));
        }

        return secteurs;
    }

    private static PointDuCircuit PointA(PointDuCircuit[] boucle, double[] cumuls, double distance)
    {
        var i = Array.FindIndex(cumuls, c => c >= distance);
        if (i <= 0)
            return boucle[Math.Max(i, 0)];

        var part = (distance - cumuls[i - 1]) / (cumuls[i] - cumuls[i - 1]);
        return new PointDuCircuit(
            boucle[i - 1].X + (boucle[i].X - boucle[i - 1].X) * part,
            boucle[i - 1].Y + (boucle[i].Y - boucle[i - 1].Y) * part);
    }

    private static PointDuCircuit VersLExterieur(PointDuCircuit point)
    {
        var dx = point.X - Centre.X;
        var dy = point.Y - Centre.Y;
        var longueur = Math.Max(Math.Sqrt(dx * dx + dy * dy), 1);
        return new PointDuCircuit(point.X + dx / longueur * EcartDuRepere, point.Y + dy / longueur * EcartDuRepere);
    }

    /// <summary>Un repère posé à gauche du tracé s'aligne à droite, et inversement ; au-dessus ou en dessous, il est centré.</summary>
    private static string Ancrage(PointDuCircuit milieu, PointDuCircuit repere)
    {
        var dx = repere.X - milieu.X;
        var dy = repere.Y - milieu.Y;
        if (Math.Abs(dx) < Math.Abs(dy) * 0.6)
            return "middle";
        return dx < 0 ? "end" : "start";
    }

    private static string Chemin(List<PointDuCircuit> points)
    {
        var chemin = new StringBuilder();
        for (var i = 0; i < points.Count; i++)
            chemin.Append(i == 0 ? 'M' : 'L').Append(Nombre(points[i].X)).Append(' ').Append(Nombre(points[i].Y));
        return chemin.ToString();
    }

    private static double Distance(PointDuCircuit a, PointDuCircuit b) =>
        Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

    private static PointDuCircuit[] Lire(int[] coordonnees) =>
        Enumerable.Range(0, coordonnees.Length / 2)
            .Select(i => new PointDuCircuit(coordonnees[2 * i], coordonnees[2 * i + 1]))
            .ToArray();
}
