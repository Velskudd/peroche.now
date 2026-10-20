namespace PerocheNow.Domain.Common;

/// <summary>
/// Classe de base de tous les objets fonctionnels identifiés du domaine.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
