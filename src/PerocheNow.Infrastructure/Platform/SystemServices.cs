using PerocheNow.Business.Ports;

namespace PerocheNow.Infrastructure.Platform;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}

/// <summary>Auteur des actions tant qu'il n'y a pas d'authentification ni de service de rôles.</summary>
internal sealed class SystemUser : ICurrentUser
{
    public string Name => "system";
}
