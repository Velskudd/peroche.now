using Microsoft.EntityFrameworkCore;
using PerocheNow.Business.Ports;
using PerocheNow.Domain.Audit;
using PerocheNow.Domain.Maintenant;
using PerocheNow.Domain.Plateformes;

namespace PerocheNow.Infrastructure.Persistance;

public sealed class PerocheNowDbContext(DbContextOptions<PerocheNowDbContext> options)
    : DbContext(options), IUniteDeTravail
{
    public DbSet<StatutDuMoment> StatutsDuMoment => Set<StatutDuMoment>();
    public DbSet<TraceAudit> TracesAudit => Set<TraceAudit>();
    public DbSet<AccesInstagram> AccesInstagram => Set<AccesInstagram>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(PerocheNowDbContext).Assembly);

    public Task EnregistrerAsync(CancellationToken annulation) => SaveChangesAsync(annulation);
}
