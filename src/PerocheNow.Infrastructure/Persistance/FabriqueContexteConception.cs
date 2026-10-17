using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PerocheNow.Infrastructure.Persistance;

/// <summary>
/// Utilisée uniquement par les outils <c>dotnet ef</c> pour générer les migrations SQL Server,
/// sans démarrer l'application web. La chaîne de connexion n'est pas utilisée pour générer une migration.
/// </summary>
public sealed class FabriqueContexteConception : IDesignTimeDbContextFactory<PerocheNowDbContext>
{
    public PerocheNowDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PerocheNowDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=PerocheNow;Trusted_Connection=True;")
            .Options;

        return new PerocheNowDbContext(options);
    }
}
