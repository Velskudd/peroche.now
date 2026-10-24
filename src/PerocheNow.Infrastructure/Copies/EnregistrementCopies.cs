using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PerocheNow.Business.Ports;

namespace PerocheNow.Infrastructure.Copies;

public static class EnregistrementCopies
{
    /// <summary>
    /// Branche les copies de lecture : le conteneur Azure Blob si la section <c>Copies</c> est complète,
    /// sinon aucune copie (le site lit alors la base à chaque fois, ce qui convient en local).
    /// </summary>
    public static IServiceCollection AjouterCopiesDeLecture(this IServiceCollection services, IConfiguration configuration)
    {
        var parametres = configuration.GetSection(CopiesOptions.Section).Get<CopiesOptions>()?.Parametres();
        if (parametres is null)
        {
            services.AddSingleton<ICopiesDeLecture, CopiesDeLectureNonConfigurees>();
            return services;
        }

        services.AddSingleton(parametres);
        services.AddHttpClient(CopiesDeLectureBlob.NomClient, client => client.Timeout = TimeSpan.FromSeconds(8));
        services.AddSingleton<ICopiesDeLecture, CopiesDeLectureBlob>();
        return services;
    }
}
