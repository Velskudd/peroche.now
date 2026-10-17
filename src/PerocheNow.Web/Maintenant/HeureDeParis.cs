using System.Globalization;

namespace PerocheNow.Web.Maintenant;

/// <summary>Écrit l'instant à la française, à l'heure de Paris : « Samedi 17 octobre, 13 h 32 ».</summary>
public static class HeureDeParis
{
    private static readonly CultureInfo Francais = CultureInfo.GetCultureInfo("fr-FR");
    private static readonly TimeZoneInfo? Fuseau =
        TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Paris", out var fuseau) ? fuseau : null;

    /// <summary>Renvoie <c>null</c> si le fuseau de Paris est inconnu de la machine.</summary>
    public static string? Formater(DateTimeOffset instant)
    {
        if (Fuseau is null)
            return null;

        var local = TimeZoneInfo.ConvertTime(instant, Fuseau);
        var jour = local.ToString("dddd d MMMM", Francais);
        return $"{char.ToUpper(jour[0], Francais)}{jour[1..]}, {local:HH} h {local:mm}";
    }
}
