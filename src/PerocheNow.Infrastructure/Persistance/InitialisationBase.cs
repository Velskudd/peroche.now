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
}
