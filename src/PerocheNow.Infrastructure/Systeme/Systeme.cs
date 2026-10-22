using PerocheNow.Business.Ports;

namespace PerocheNow.Infrastructure.Systeme;

internal sealed class HorlogeSysteme : IHorloge
{
    public DateTimeOffset Instant => DateTimeOffset.UtcNow;
}

/// <summary>Auteur par défaut des actions. Le site le remplace par la personne connectée à l'administration.</summary>
internal sealed class UtilisateurSysteme : IUtilisateurCourant
{
    public string Nom => "système";
}
