using Microsoft.Extensions.DependencyInjection;
using PerocheNow.Business.Contenus;
using PerocheNow.Business.Contenus.Instagram;
using PerocheNow.Business.Mediateur;
using PerocheNow.Business.Ports;

namespace PerocheNow.Infrastructure.Contenus;

/// <summary>
/// Source Instagram : lit l'accès dans sa copie de lecture (dans sa propre portée, car la source vit toute
/// la durée du site), puis les publications du compte. La base n'est pas réveillée.
/// Sans accès valide, la source ne renvoie rien.
/// </summary>
public sealed class SourceInstagram(IServiceScopeFactory portees, IApiInstagram instagram, IHorloge horloge) : ISourceDeContenus
{
    public async Task<IReadOnlyList<Contenu>> ObtenirAsync(CancellationToken annulation)
    {
        await using var portee = portees.CreateAsyncScope();
        var acces = await portee.ServiceProvider.GetRequiredService<IMediateur>()
            .EnvoyerAsync(new ObtenirAccesInstagramPublie(), annulation);

        if (acces is null || acces.EstExpire(horloge.Instant))
            return [];

        return await instagram.ObtenirPublicationsAsync(acces.Jeton, annulation);
    }
}
