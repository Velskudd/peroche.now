using PerocheNow.Business.Ports;

namespace PerocheNow.Infrastructure.Systeme;

internal sealed class HorlogeSysteme : IHorloge
{
    public DateTimeOffset Instant => DateTimeOffset.UtcNow;
}

/// <summary>Auteur des actions tant qu'il n'y a pas d'authentification ni de service de rôles.</summary>
internal sealed class UtilisateurSysteme : IUtilisateurCourant
{
    public string Nom => "système";
}
