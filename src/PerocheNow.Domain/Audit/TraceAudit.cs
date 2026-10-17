using PerocheNow.Domain.Commun;

namespace PerocheNow.Domain.Audit;

/// <summary>
/// Trace d'une action effectuée sur un objet fonctionnel : qui, quoi, sur quel objet et quand.
/// </summary>
public sealed class TraceAudit : Entite
{
    public string Action { get; private set; } = string.Empty;
    public string Auteur { get; private set; } = string.Empty;
    public string? TypeObjet { get; private set; }
    public Guid? IdObjet { get; private set; }
    public string? Details { get; private set; }
    public DateTimeOffset Le { get; private set; }

    // Constructeur réservé à l'ORM.
    private TraceAudit() { }

    public TraceAudit(string action, string auteur, DateTimeOffset le, string? typeObjet = null, Guid? idObjet = null, string? details = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new RegleMetierException("Une trace d'audit doit nommer l'action.");

        Action = action;
        Auteur = string.IsNullOrWhiteSpace(auteur) ? "système" : auteur;
        Le = le;
        TypeObjet = typeObjet;
        IdObjet = idObjet;
        Details = details;
    }
}
