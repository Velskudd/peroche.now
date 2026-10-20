using Microsoft.EntityFrameworkCore;
using PerocheNow.Business.Ports;
using PerocheNow.Domain.Audit;
using PerocheNow.Domain.Status;

namespace PerocheNow.Infrastructure.Persistence;

internal sealed class CurrentStatusRepository(PerocheNowDbContext db) : ICurrentStatusRepository
{
    public Task<CurrentStatus?> GetAsync(CancellationToken cancellationToken)
        => db.CurrentStatuses.OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);

    public void Add(CurrentStatus status) => db.CurrentStatuses.Add(status);
}

internal sealed class AuditEntryRepository(PerocheNowDbContext db) : IAuditEntryRepository
{
    public void Add(AuditEntry entry) => db.AuditEntries.Add(entry);
}
