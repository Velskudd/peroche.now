using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PerocheNow.Business.Audit;
using PerocheNow.Business.Mediation;

namespace PerocheNow.Business;

public static class BusinessRegistration
{
    /// <summary>
    /// Enregistre le médiateur, tous les gestionnaires de la couche Business et les services transverses.
    /// </summary>
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        services.AddScoped<IMediator, Mediator>();
        services.AddHandlers(typeof(BusinessRegistration).Assembly);

        // Comportements du pipeline, du plus extérieur au plus intérieur :
        // l'unité de travail enregistre ce que la commande et l'audit ont préparé.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));

        services.AddScoped<IAuditService, AuditService>();
        return services;
    }

    /// <summary>Enregistre chaque classe qui implémente <see cref="IHandler{TRequest, TResponse}"/>.</summary>
    public static IServiceCollection AddHandlers(this IServiceCollection services, Assembly assembly)
    {
        var handlers = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandler<,>))
                .Select(i => (Contract: i, Implementation: t)));

        foreach (var (contract, implementation) in handlers)
            services.AddScoped(contract, implementation);

        return services;
    }
}
