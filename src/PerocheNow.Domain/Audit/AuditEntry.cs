using PerocheNow.Domain.Common;

namespace PerocheNow.Domain.Audit;

/// <summary>
/// Trace d'une action effectuée sur un objet fonctionnel : qui, quoi, sur quel objet et quand.
/// </summary>
public sealed class AuditEntry : Entity
{
    public string Action { get; private set; } = string.Empty;
    public string Author { get; private set; } = string.Empty;
    public string? ObjectType { get; private set; }
    public Guid? ObjectId { get; private set; }
    public string? Details { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    // Constructeur réservé à l'ORM.
    private AuditEntry() { }

    public AuditEntry(string action, string author, DateTimeOffset occurredAt, string? objectType = null, Guid? objectId = null, string? details = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new BusinessRuleException("Une trace d'audit doit nommer l'action.");

        Action = action;
        Author = string.IsNullOrWhiteSpace(author) ? "system" : author;
        OccurredAt = occurredAt;
        ObjectType = objectType;
        ObjectId = objectId;
        Details = details;
    }
}
