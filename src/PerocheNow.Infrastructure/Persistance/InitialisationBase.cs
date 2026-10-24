using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PerocheNow.Business.CopiesDeLecture;
using PerocheNow.Business.Maintenant;
using PerocheNow.Business.Mediateur;
using PerocheNow.Business.Ports;

namespace PerocheNow.Infrastructure.Persistance;

public static class InitialisationBase
{
    private const string SansMigration = "schema-sans-migration";

    /// <summary>
    /// Met la base à jour puis, si elle est vide, y dépose le premier statut du moment.
    /// Les données initiales passent par le médiateur, donc par l'audit, comme n'importe quelle commande.
    /// <para>
    /// Le site redémarre à chaque visite après une période calme : pour ne pas réveiller la base à chaque fois,
    /// la dernière migration appliquée est notée dans une copie de lecture. Tant qu'elle correspond à celle
    /// du site, la base n'est pas touchée ; elle ne l'est donc qu'au premier démarrage d'une nouvelle version.
    /// </para>
    /// </summary>
    public static async Task InitialiserBaseAsync(this IServiceProvider services, CancellationToken annulation = default)
    {
        await using var portee = services.CreateAsyncScope();
        var db = portee.ServiceProvider.GetRequiredService<PerocheNowDbContext>();
        var copies = portee.ServiceProvider.GetRequiredService<ICopiesDeLecture>();
        var journal = portee.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(InitialisationBase));

        // Les migrations sont écrites pour SQL Server. En local sur SQLite, ou tant qu'aucune migration
        // n'a encore été générée, on crée simplement le schéma à partir du modèle.
        var avecMigrations = db.Database.IsSqlServer() && db.Database.GetMigrations().Any();
        var version = avecMigrations ? db.Database.GetMigrations().Last() : SansMigration;

        if (await DejaPrepareeAsync(copies, version, journal, annulation))
        {
            journal.LogInformation("Base déjà prête pour cette version du site : elle reste en pause.");
            return;
        }

        if (avecMigrations)
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

        if (!await db.StatutsDuMoment.AnyAsync(annulation))
        {
            journal.LogInformation("Base vide : dépôt du premier statut du moment.");
            var mediateur = portee.ServiceProvider.GetRequiredService<IMediateur>();
            await mediateur.EnvoyerAsync(StatutInitial.Commande, annulation);
        }

        var horloge = portee.ServiceProvider.GetRequiredService<IHorloge>();
        await copies.PublierAsync(NomsDesCopies.EtatDeLaBase, new EtatDeLaBase(version, horloge.Instant), annulation);
    }

    /// <summary>Vrai si la copie dit que la base a déjà reçu cette migration. En cas de doute, on prépare.</summary>
    private static async Task<bool> DejaPrepareeAsync(ICopiesDeLecture copies, string version, ILogger journal, CancellationToken annulation)
    {
        try
        {
            var etat = await copies.LireAsync<EtatDeLaBase>(NomsDesCopies.EtatDeLaBase, annulation);
            return string.Equals(etat?.DerniereMigration, version, StringComparison.Ordinal);
        }
        catch (Exception erreur) when (!annulation.IsCancellationRequested)
        {
            journal.LogWarning(erreur, "État de la base illisible : préparation de la base par précaution.");
            return false;
        }
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

/// <summary>Ce que la copie de lecture retient de la base : sa dernière migration, et quand elle a été préparée.</summary>
internal sealed record EtatDeLaBase(string DerniereMigration, DateTimeOffset PrepareeLe);
