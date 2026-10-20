using Microsoft.Extensions.DependencyInjection;
using PerocheNow.Business.Ports;
using PerocheNow.Domain.Audit;
using PerocheNow.Domain.Status;
using IMediator = PerocheNow.Business.Mediation.IMediator;

namespace PerocheNow.Business.Tests;

// Doublures des ports : elles remplacent l'Infrastructure pour tester la couche Business seule.

internal sealed class FakeStatuses : ICurrentStatusRepository
{
    public CurrentStatus? Status { get; set; }

    public Task<CurrentStatus?> GetAsync(CancellationToken cancellationToken) => Task.FromResult(Status);

    public void Add(CurrentStatus status) => Status = status;
}

internal sealed class FakeAuditEntries : IAuditEntryRepository
{
    public List<AuditEntry> Entries { get; } = [];

    public void Add(AuditEntry entry) => Entries.Add(entry);
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset Now { get; set; } = now;
}

internal sealed class FixedUser(string name) : ICurrentUser
{
    public string Name { get; } = name;
}

/// <summary>La couche Business complète, branchée sur les doublures.</summary>
internal sealed class TestBench : IDisposable
{
    public static readonly DateTimeOffset Now = new(2026, 10, 17, 11, 32, 0, TimeSpan.Zero);

    private readonly ServiceProvider _provider;

    public FakeStatuses Statuses { get; } = new();
    public FakeAuditEntries AuditEntries { get; } = new();
    public FakeUnitOfWork UnitOfWork { get; } = new();
    public FixedClock Clock { get; } = new(Now);

    public TestBench(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddBusiness();
        services.AddSingleton<ICurrentStatusRepository>(Statuses);
        services.AddSingleton<IAuditEntryRepository>(AuditEntries);
        services.AddSingleton<IUnitOfWork>(UnitOfWork);
        services.AddSingleton<IClock>(Clock);
        services.AddSingleton<ICurrentUser>(new FixedUser("alexandre"));
        configure?.Invoke(services);

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    public static IMediator Mediator(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<IMediator>();

    public IServiceScope NewScope() => _provider.CreateScope();

    public void Dispose() => _provider.Dispose();
}
