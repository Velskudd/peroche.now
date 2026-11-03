using Microsoft.EntityFrameworkCore;
using PerocheNow.Business.Ports;
using PerocheNow.Domain.Audit;
using PerocheNow.Domain.Maintenant;
using PerocheNow.Domain.Plateformes;
using PerocheNow.Domain.Textes;

namespace PerocheNow.Infrastructure.Persistance;

internal sealed class StatutDuMomentRepository(PerocheNowDbContext db) : IStatutDuMomentRepository
{
    public Task<StatutDuMoment?> ObtenirAsync(CancellationToken annulation)
        => db.StatutsDuMoment.OrderBy(s => s.Id).FirstOrDefaultAsync(annulation);

    public void Ajouter(StatutDuMoment statut) => db.StatutsDuMoment.Add(statut);
}

internal sealed class TraceAuditRepository(PerocheNowDbContext db) : ITraceAuditRepository
{
    public void Ajouter(TraceAudit trace) => db.TracesAudit.Add(trace);
}

internal sealed class AccesInstagramRepository(PerocheNowDbContext db) : IAccesInstagramRepository
{
    public Task<AccesInstagram?> ObtenirAsync(CancellationToken annulation)
        => db.AccesInstagram.OrderBy(a => a.Id).FirstOrDefaultAsync(annulation);

    public void Ajouter(AccesInstagram acces) => db.AccesInstagram.Add(acces);
}

internal sealed class TextesDuSiteRepository(PerocheNowDbContext db) : ITextesDuSiteRepository
{
    public async Task<IReadOnlyList<TexteDuSite>> ListerAsync(CancellationToken annulation)
        => await db.TextesDuSite.OrderBy(t => t.Cle).ToListAsync(annulation);

    public void Ajouter(TexteDuSite texte) => db.TextesDuSite.Add(texte);

    public void Supprimer(TexteDuSite texte) => db.TextesDuSite.Remove(texte);
}
