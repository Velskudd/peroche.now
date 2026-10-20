using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PerocheNow.Business.Mediation;
using PerocheNow.Business.Status;

namespace PerocheNow.Infrastructure.Persistence;

public static class DatabaseInitialization
{
    /// <summary>
    /// Met la base à jour puis, si elle est vide, y dépose le premier statut du moment.
    /// Les données initiales passent par le médiateur, donc par l'audit, comme n'importe quelle commande.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PerocheNowDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitialization));

        // Les migrations sont écrites pour SQL Server. En local sur SQLite, ou tant qu'aucune migration
        // n'a encore été générée, on crée simplement le schéma à partir du modèle.
        if (db.Database.IsSqlServer() && db.Database.GetMigrations().Any())
        {
            await DropSchemaWithoutHistoryAsync(db, logger, cancellationToken);
            logger.LogInformation("Application des migrations SQL Server.");
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            logger.LogInformation("Création du schéma à partir du modèle.");
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (await db.CurrentStatuses.AnyAsync(cancellationToken))
            return;

        logger.LogInformation("Base vide : dépôt du premier statut du moment.");
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.SendAsync(InitialStatus.Command, cancellationToken);
    }

    /// <summary>
    /// Passage unique aux migrations. Les premières versions du site créaient le schéma sans migration
    /// (<c>EnsureCreated</c>) : ces tables ne contiennent que le statut initial et sa trace, recréés juste après.
    /// On ne supprime que dans ce cas précis : tables présentes ET aucun historique de migrations.
    /// Une fois les migrations appliquées, l'historique existe et cette étape ne fait plus rien.
    /// Les noms de tables sont ceux de ce tout premier schéma, d'où le français.
    /// </summary>
    private static async Task DropSchemaWithoutHistoryAsync(PerocheNowDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        const string schemaWithoutHistory = """
            SELECT CASE WHEN OBJECT_ID(N'[__EFMigrationsHistory]', N'U') IS NULL
                         AND OBJECT_ID(N'[StatutDuMoment]', N'U') IS NOT NULL
                        THEN 1 ELSE 0 END AS [Value]
            """;

        var mustDrop = await db.Database.SqlQueryRaw<int>(schemaWithoutHistory).SingleAsync(cancellationToken);
        if (mustDrop == 0)
            return;

        logger.LogWarning("Schéma créé sans migration : suppression avant de le recréer par les migrations.");
        await db.Database.ExecuteSqlRawAsync("""
            DROP TABLE IF EXISTS [LigneDuMoment];
            DROP TABLE IF EXISTS [StatutDuMoment];
            DROP TABLE IF EXISTS [TraceAudit];
            """, cancellationToken);
    }
}
