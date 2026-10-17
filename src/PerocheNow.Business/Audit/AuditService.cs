using PerocheNow.Business.Ports;
using PerocheNow.Domain.Audit;

namespace PerocheNow.Business.Audit;

/// <summary>Trace ce qui est fait sur les objets fonctionnels de l'application.</summary>
public interface IAuditService
{
    void Tracer(string action, string? typeObjet = null, Guid? idObjet = null, string? details = null);
}

public sealed class AuditService(
    ITraceAuditRepository traces,
    IHorloge horloge,
    IUtilisateurCourant utilisateur) : IAuditService
{
    public void Tracer(string action, string? typeObjet = null, Guid? idObjet = null, string? details = null)
        => traces.Ajouter(new TraceAudit(action, utilisateur.Nom, horloge.Instant, typeObjet, idObjet, details));
}
