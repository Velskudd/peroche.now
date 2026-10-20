using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PerocheNow.Business;
using PerocheNow.Business.Mediation;
using PerocheNow.Business.Status;
using PerocheNow.Domain.Status;
using PerocheNow.Infrastructure.Persistence;

namespace PerocheNow.Infrastructure.Tests;

/// <summary>
/// Vérifie le vrai câblage EF Core de bout en bout, sur une base SQLite en mémoire :
/// configuration, initialisation, commandes passées par le médiateur et relecture dans une nouvelle portée.
/// </summary>
public sealed class PersistenceTests : IAsyncLifetime
{
    private readonly string _connectionString = $"Data Source=perochenow-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    // Une base SQLite en mémoire disparaît avec sa dernière connexion : celle-ci la garde en vie le temps du test.
    private SqliteConnection _keepAlive = null!;
    private ServiceProvider _services = null!;

    public async Task InitializeAsync()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        await _keepAlive.OpenAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PerocheNow"] = _connectionString,
                ["Database:Provider"] = "Sqlite",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBusiness();
        services.AddInfrastructure(configuration);
        _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await _services.InitializeDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        await _keepAlive.DisposeAsync();
    }

    private async Task<T> InScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = _services.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    private Task<CurrentStatusDto?> ReadStatusAsync() =>
        InScopeAsync(sp => sp.GetRequiredService<IMediator>().SendAsync(new GetCurrentStatus()));

    private const string CountStatuses = "SELECT COUNT(*) AS \"Value\" FROM \"CurrentStatus\"";
    private const string CountLines = "SELECT COUNT(*) AS \"Value\" FROM \"StatusLine\"";
    private const string CountAuditEntries = "SELECT COUNT(*) AS \"Value\" FROM \"AuditEntry\"";

    private Task<int> CountAsync(string sql) =>
        InScopeAsync(sp => sp.GetRequiredService<PerocheNowDbContext>().Database
            .SqlQueryRaw<int>(sql)
            .SingleAsync());

    [Fact]
    public async Task An_empty_database_receives_the_initial_status_and_its_audit_entry()
    {
        var status = await ReadStatusAsync();

        Assert.NotNull(status);
        Assert.Equal(InitialStatus.Command.Headline, status.Headline);
        Assert.Equal(
            InitialStatus.Command.Lines.Select(l => new StatusLineDto(l.Text, l.Section)),
            status.Lines);
        Assert.Equal(InitialStatus.Command.Goal, status.NextGoal?.Label);
        Assert.Equal(InitialStatus.Command.GoalDate, status.NextGoal?.TargetDate);

        var entry = await InScopeAsync(sp => sp.GetRequiredService<PerocheNowDbContext>().AuditEntries.SingleAsync());
        Assert.Equal(nameof(SetCurrentStatus), entry.Action);
        Assert.Equal("system", entry.Author);
    }

    [Fact]
    public async Task Running_the_initialization_again_duplicates_nothing()
    {
        await _services.InitializeDatabaseAsync();

        Assert.Equal(1, await CountAsync(CountStatuses));
        Assert.Equal(1, await CountAsync(CountAuditEntries));
        Assert.Equal(InitialStatus.Command.Lines.Count, await CountAsync(CountLines));
    }

    [Fact]
    public async Task Set_replaces_the_lines_and_the_goal_in_the_database()
    {
        await InScopeAsync(sp => sp.GetRequiredService<IMediator>().SendAsync(
            new SetCurrentStatus(
                "Je reviens de la Test Week.",
                [new LineInput("Débrief des données", Section.CurrentProject), new LineInput("Montage du vlog", Section.Content)],
                "RCN 2027",
                new DateOnly(2027, 4, 10))));

        var status = await ReadStatusAsync();

        Assert.NotNull(status);
        Assert.Equal("Je reviens de la Test Week.", status.Headline);
        StatusLineDto[] expectedLines =
        [
            new("Débrief des données", Section.CurrentProject),
            new("Montage du vlog", Section.Content),
        ];
        Assert.Equal(expectedLines, status.Lines);
        Assert.Equal(new GoalDto("RCN 2027", new DateOnly(2027, 4, 10)), status.NextGoal);

        // Les anciennes lignes sont supprimées, pas seulement détachées.
        Assert.Equal(2, await CountAsync(CountLines));
        Assert.Equal(2, await CountAsync(CountAuditEntries));
    }

    [Fact]
    public async Task Removing_the_goal_is_persisted()
    {
        await InScopeAsync(sp => sp.GetRequiredService<IMediator>().SendAsync(
            new SetCurrentStatus("Pause entre deux projets.", [], Goal: null, GoalDate: null)));

        var status = await ReadStatusAsync();

        Assert.NotNull(status);
        Assert.Null(status.NextGoal);
        Assert.Empty(status.Lines);
    }

    [Fact]
    public void The_model_translates_into_a_SQL_Server_schema()
    {
        var options = new DbContextOptionsBuilder<PerocheNowDbContext>()
            .UseSqlServer("Server=localhost;Database=PerocheNow;Trusted_Connection=True;")
            .Options;
        using var db = new PerocheNowDbContext(options);

        var script = db.Database.GenerateCreateScript();

        Assert.Contains("CREATE TABLE [CurrentStatus]", script);
        Assert.Contains("CREATE TABLE [StatusLine]", script);
        Assert.Contains("CREATE TABLE [AuditEntry]", script);
        Assert.Contains("[GoalLabel] nvarchar(80)", script);
        Assert.Contains("[Headline] nvarchar(140) NOT NULL", script);
        Assert.Contains("[Section] nvarchar(30) NOT NULL", script);
    }

    [Fact]
    public void The_migrations_match_the_model()
    {
        var options = new DbContextOptionsBuilder<PerocheNowDbContext>()
            .UseSqlServer("Server=localhost;Database=PerocheNow;Trusted_Connection=True;")
            .Options;
        using var db = new PerocheNowDbContext(options);

        Assert.False(db.Database.HasPendingModelChanges());
    }
}
