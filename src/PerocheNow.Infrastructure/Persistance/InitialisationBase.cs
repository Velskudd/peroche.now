using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PerocheNow.Business.Maintenant;
using PerocheNow.Business.Mediateur;

namespace PerocheNow.Infrastructure.Persistance;

public static class InitialisationBase
{
    /// <summary>
    /// Met la base à jour puis, si elle est vide, y dépose le premier statut du moment.
    /// Les données initiales passent par le médiateur, donc par l'audit, comme n'importe quelle commande.
    /// </summary>
    public static async Task InitialiserBaseAsync(this IServiceProvider services, CancellationToken annulation = default)
    {
        await using var portee = services.CreateAsyncScope();
        var db = portee.ServiceProvider.GetRequiredService<PerocheNowDbContext>();
        var journal = portee.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(InitialisationBase));

        // Les migrations sont écrites pour SQL Server. En local sur SQLite, ou tant qu'aucune migration
        // n'a encore été générée, on crée simplement le schéma à partir du modèle.
        if (db.Database.IsSqlServer() && db.Database.GetMigrations().Any())
        {
            await AbandonnerSchemaSansHistoriqueAsync(db, journal, annulation);
            journal.LogInformation("Application des migrations SQL Server.");
            await db.Database.MigrateAsync(annulation);
        }
        else
        {
            journal.LogInformation("Création du schéma à partir du modèle.");
            await db.Database.EnsureCreatedAsync(annulation);
        }

        if (await db.StatutsDuMoment.AnyAsync(annulation))
            return;

        journal.LogInformation("Base vide : dépôt du premier statut du moment.");
        var mediateur = portee.ServiceProvider.GetRequiredService<IMediateur>();
        await mediateur.EnvoyerAsync(StatutInitial.Commande, annulation);
    }

    /// <summary>
    /// Passage unique aux migrations. Les premières versions du site créaient le schéma sans migration
    /// (<c>EnsureCreated</c>) : ces tables ne contiennent que le statut initial et sa trace, recréés juste après.
    /// On ne supprime que dans ce cas précis : tables présentes ET aucun historique de migrations.
    /// Une fois les migrations appliquées, l'historique existe et cette étape ne fait plus rien.
    /// </summary>
    private static async Task AbandonnerSchemaSansHistoriqueAsync(PerocheNowDbContext db, ILogger journal, CancellationToken annulation)
    {
        const string schemaSansHistorique = """
            SELECT CASE WHEN OBJECT_ID(N'[__EFMigrationsHistory]', N'U') IS NULL
                         AND OBJECT_ID(N'[StatutDuMoment]', N'U') IS NOT NULL
                        THEN 1 ELSE 0 END AS [Value]
            """;

        var aAbandonner = await db.Database.SqlQueryRaw<int>(schemaSansHistorique).SingleAsync(annulation);
        if (aAbandonner == 0)
            return;

        journal.LogWarning("Schéma créé sans migration : suppression avant de le recréer par les migrations.");
        await db.Database.ExecuteSqlRawAsync("""
            DROP TABLE IF EXISTS [LigneDuMoment];
            DROP TABLE IF EXISTS [StatutDuMoment];
            DROP TABLE IF EXISTS [TraceAudit];
            """, annulation);
    }
}
