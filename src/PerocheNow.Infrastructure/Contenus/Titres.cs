using System.Text.RegularExpressions;

namespace PerocheNow.Infrastructure.Contenus;

/// <summary>Mise au propre des titres et légendes venus des plateformes.</summary>
public static partial class Titres
{
    /// <summary>Au-delà, une légende Instagram est coupée : le site n'en affiche que le début.</summary>
    public const int LongueurMax = 120;

    /// <summary>
    /// Retire les mots-dièses, utiles sur les plateformes mais bruyants sur le site :
    /// « Au millimètre | #ap15fr sur #Twitch » devient « Au millimètre ».
    /// Un numéro d'épisode (« #4 ») n'est pas un mot-dièse et reste.
    /// </summary>
    public static string Nettoyer(string titre)
    {
        var barre = titre.IndexOf(" | ", StringComparison.Ordinal);
        var utile = barre > 0 && MotDiese().IsMatch(titre[barre..]) ? titre[..barre] : titre;

        var propre = Espaces().Replace(MotDiese().Replace(utile, " "), " ").Trim();
        return propre.Length > 0 ? propre : titre.Trim();
    }

    /// <summary>La première ligne d'une légende, nettoyée et coupée proprement si elle est trop longue.</summary>
    public static string DepuisLegende(string? legende, string parDefaut)
    {
        var premiereLigne = legende?.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(l => MotDiese().Replace(l, string.Empty).Trim().Length > 0);
        if (premiereLigne is null)
            return parDefaut;

        var propre = Nettoyer(premiereLigne);
        if (propre.Length <= LongueurMax)
            return propre;

        var coupe = propre[..LongueurMax];
        var dernierEspace = coupe.LastIndexOf(' ');
        return (dernierEspace > LongueurMax / 2 ? coupe[..dernierEspace] : coupe).TrimEnd(' ', ',', ';', ':') + "…";
    }

    /// <summary>Les mots-dièses d'un texte, sans « # » et sans doublon : « #NUR24 #nur24 » donne « NUR24 ».</summary>
    public static IReadOnlyCollection<string> Etiquettes(string? texte) =>
        texte is null
            ? []
            : MotDiese().Matches(texte)
                .Select(m => m.Value[1..])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

    [GeneratedRegex(@"#\p{L}[\p{L}\p{N}_]*", RegexOptions.None, matchTimeoutMilliseconds: 200)]
    private static partial Regex MotDiese();

    [GeneratedRegex(@"\s{2,}", RegexOptions.None, matchTimeoutMilliseconds: 200)]
    private static partial Regex Espaces();
}
