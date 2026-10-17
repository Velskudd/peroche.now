using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PerocheNow.Business.Audit;
using PerocheNow.Business.Mediateur;

namespace PerocheNow.Business;

public static class EnregistrementBusiness
{
    /// <summary>
    /// Enregistre le médiateur, tous les gestionnaires de la couche Business et les services transverses.
    /// </summary>
    public static IServiceCollection AjouterBusiness(this IServiceCollection services)
    {
        services.AddScoped<IMediateur, Mediateur.Mediateur>();
        services.AjouterGestionnaires(typeof(EnregistrementBusiness).Assembly);

        // Comportements du pipeline, du plus extérieur au plus intérieur :
        // l'unité de travail enregistre ce que la commande et l'audit ont préparé.
        services.AddScoped(typeof(IComportement<,>), typeof(ComportementUniteDeTravail<,>));
        services.AddScoped(typeof(IComportement<,>), typeof(ComportementAudit<,>));

        services.AddScoped<IAuditService, AuditService>();
        return services;
    }

    /// <summary>Enregistre chaque classe qui implémente <see cref="IGestionnaire{TRequete, TReponse}"/>.</summary>
    public static IServiceCollection AjouterGestionnaires(this IServiceCollection services, Assembly assembly)
    {
        var gestionnaires = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IGestionnaire<,>))
                .Select(i => (Contrat: i, Implementation: t)));

        foreach (var (contrat, implementation) in gestionnaires)
            services.AddScoped(contrat, implementation);

        return services;
    }
}
