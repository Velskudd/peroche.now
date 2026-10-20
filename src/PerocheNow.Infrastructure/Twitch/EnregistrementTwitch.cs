using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PerocheNow.Business.Ports;

namespace PerocheNow.Infrastructure.Twitch;

public static class EnregistrementTwitch
{
    /// <summary>
    /// Branche la source du direct : l'adaptateur Twitch si la section <c>Twitch</c> est complète,
    /// sinon une source inactive (pas de pastille, aucun appel à Twitch).
    /// </summary>
    public static IServiceCollection AjouterDirectTwitch(this IServiceCollection services, IConfiguration configuration)
    {
        var parametres = configuration.GetSection(TwitchOptions.Section).Get<TwitchOptions>()?.Parametres();
        if (parametres is null)
        {
            services.AddSingleton<ISourceDuDirect, SourceDuDirectInactive>();
            return services;
        }

        services.AddSingleton(parametres);
        services.AddHttpClient(SourceDuDirectTwitch.NomClient, client => client.Timeout = TimeSpan.FromSeconds(5));
        services.AddSingleton<JetonTwitch>();
        services.AddScoped<ISourceDuDirect, SourceDuDirectTwitch>();
        return services;
    }
}
