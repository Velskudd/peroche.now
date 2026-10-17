namespace PerocheNow.Domain.Commun;

/// <summary>
/// Classe de base de tous les objets fonctionnels identifiés du domaine.
/// </summary>
public abstract class Entite
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
