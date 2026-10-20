using Microsoft.EntityFrameworkCore;
using PerocheNow.Business.Ports;
using PerocheNow.Domain.Audit;
using PerocheNow.Domain.Status;

namespace PerocheNow.Infrastructure.Persistence;

public sealed class PerocheNowDbContext(DbContextOptions<PerocheNowDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<CurrentStatus> CurrentStatuses => Set<CurrentStatus>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(PerocheNowDbContext).Assembly);

    public Task SaveAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);
}
