using System.Globalization;

namespace PerocheNow.Web.Status;

/// <summary>Écrit l'instant à la française, à l'heure de Paris : « Samedi 17 octobre, 13 h 32 ».</summary>
public static class ParisTime
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");
    private static readonly TimeZoneInfo? ParisTimeZone =
        TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Paris", out var timeZone) ? timeZone : null;

    /// <summary>Renvoie <c>null</c> si le fuseau de Paris est inconnu de la machine.</summary>
    public static string? Format(DateTimeOffset instant)
    {
        if (ParisTimeZone is null)
            return null;

        var local = TimeZoneInfo.ConvertTime(instant, ParisTimeZone);
        var day = local.ToString("dddd d MMMM", French);
        return $"{char.ToUpper(day[0], French)}{day[1..]}, {local:HH} h {local:mm}";
    }
}
