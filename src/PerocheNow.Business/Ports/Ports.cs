using PerocheNow.Domain.Audit;
using PerocheNow.Domain.Status;

namespace PerocheNow.Business.Ports;

// Les ports décrivent ce dont la couche Business a besoin.
// Leurs implémentations vivent dans l'Infrastructure, qui dépend de Business et jamais l'inverse.

/// <summary>Accès au statut du moment. Il n'en existe qu'un seul à la fois.</summary>
public interface ICurrentStatusRepository
{
    Task<CurrentStatus?> GetAsync(CancellationToken cancellationToken);
    void Add(CurrentStatus status);
}

/// <summary>Stockage des traces d'audit.</summary>
public interface IAuditEntryRepository
{
    void Add(AuditEntry entry);
}

/// <summary>Valide en une fois toutes les modifications en attente.</summary>
public interface IUnitOfWork
{
    Task SaveAsync(CancellationToken cancellationToken);
}

/// <summary>L'heure courante, isolée pour pouvoir la maîtriser dans les tests.</summary>
public interface IClock
{
    DateTimeOffset Now { get; }
}

/// <summary>Qui agit. Renverra l'utilisateur connecté quand le service de rôles sera en place.</summary>
public interface ICurrentUser
{
    string Name { get; }
}
