using PerocheNow.Business.Ports;
using PerocheNow.Domain.Audit;

namespace PerocheNow.Business.Audit;

/// <summary>Trace ce qui est fait sur les objets fonctionnels de l'application.</summary>
public interface IAuditService
{
    void Record(string action, string? objectType = null, Guid? objectId = null, string? details = null);
}

public sealed class AuditService(
    IAuditEntryRepository entries,
    IClock clock,
    ICurrentUser user) : IAuditService
{
    public void Record(string action, string? objectType = null, Guid? objectId = null, string? details = null)
        => entries.Add(new AuditEntry(action, user.Name, clock.Now, objectType, objectId, details));
}
