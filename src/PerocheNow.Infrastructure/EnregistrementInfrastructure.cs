using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PerocheNow.Business.Ports;
using PerocheNow.Infrastructure.Contenus;
using PerocheNow.Infrastructure.Persistance;
using PerocheNow.Infrastructure.Systeme;
using PerocheNow.Infrastructure.Twitch;

namespace PerocheNow.Infrastructure;

public static class EnregistrementInfrastructure
{
    /// <summary>
    /// Branche les adaptateurs : base de données, repositories, horloge, utilisateur courant, direct Twitch et contenus.
    /// <para>
    /// Configuration attendue : <c>ConnectionStrings:PerocheNow</c> et <c>BaseDeDonnees:Fournisseur</c>
    /// (<c>SqlServer</c> par défaut, <c>Sqlite</c> pour travailler en local sans serveur).
    /// </para>
    /// </summary>
    public static IServiceCollection AjouterInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var chaine = configuration.GetConnectionString("PerocheNow")
            ?? throw new InvalidOperationException("La chaîne de connexion « PerocheNow » est absente de la configuration.");
        var fournisseur = configuration["BaseDeDonnees:Fournisseur"] ?? "SqlServer";

        services.AddDbContext<PerocheNowDbContext>(options =>
        {
            if (string.Equals(fournisseur, "Sqlite", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(chaine);
            else
                options.UseSqlServer(chaine, sql => sql.EnableRetryOnFailure());
        });

        services.AddScoped<IUniteDeTravail>(sp => sp.GetRequiredService<PerocheNowDbContext>());
        services.AddScoped<IStatutDuMomentRepository, StatutDuMomentRepository>();
        services.AddScoped<ITraceAuditRepository, TraceAuditRepository>();
        services.AddScoped<IAccesInstagramRepository, AccesInstagramRepository>();
        services.AddSingleton<IHorloge, HorlogeSysteme>();
        services.AddSingleton<IUtilisateurCourant, UtilisateurSysteme>();
        services.AjouterDirectTwitch(configuration);
        services.AjouterContenus(configuration);

        return services;
    }
}
