using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PerocheNow.Business.Ports;
using PerocheNow.Infrastructure.Persistence;
using PerocheNow.Infrastructure.Platform;

namespace PerocheNow.Infrastructure;

public static class InfrastructureRegistration
{
    /// <summary>
    /// Branche les adaptateurs : base de données, repositories, horloge et utilisateur courant.
    /// <para>
    /// Configuration attendue : <c>ConnectionStrings:PerocheNow</c> et <c>Database:Provider</c>
    /// (<c>SqlServer</c> par défaut, <c>Sqlite</c> pour travailler en local sans serveur).
    /// </para>
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PerocheNow")
            ?? throw new InvalidOperationException("La chaîne de connexion « PerocheNow » est absente de la configuration.");
        var provider = configuration["Database:Provider"] ?? "SqlServer";

        services.AddDbContext<PerocheNowDbContext>(options =>
        {
            if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(connectionString);
            else
                options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PerocheNowDbContext>());
        services.AddScoped<ICurrentStatusRepository, CurrentStatusRepository>();
        services.AddScoped<IAuditEntryRepository, AuditEntryRepository>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ICurrentUser, SystemUser>();

        return services;
    }
}
