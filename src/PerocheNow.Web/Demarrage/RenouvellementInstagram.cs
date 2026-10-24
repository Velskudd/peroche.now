using PerocheNow.Business.Contenus.Instagram;
using PerocheNow.Business.Mediateur;
using PerocheNow.Business.Ports;

namespace PerocheNow.Web.Demarrage;

/// <summary>
/// Prolonge le jeton Instagram quand il a plus d'une semaine : peu après chaque démarrage du site
/// (il se réveille à chaque visite), puis toutes les douze heures tant qu'il tourne.
/// La vérification se fait sur la copie de lecture : la base n'est réveillée que le jour du renouvellement.
/// Rien n'est tracé quand il n'y a rien à faire.
/// </summary>
public sealed class RenouvellementInstagram(
    IServiceScopeFactory portees,
    IHorloge horloge,
    ILogger<RenouvellementInstagram> journal) : BackgroundService
{
    private static readonly TimeSpan PremierPassage = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan Intervalle = TimeSpan.FromHours(12);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(PremierPassage, stoppingToken);

        using var minuteur = new PeriodicTimer(Intervalle);
        do
        {
            await VerifierAsync(stoppingToken);
        }
        while (await minuteur.WaitForNextTickAsync(stoppingToken));
    }

    private async Task VerifierAsync(CancellationToken arret)
    {
        try
        {
            await using var portee = portees.CreateAsyncScope();
            var mediateur = portee.ServiceProvider.GetRequiredService<IMediateur>();

            var acces = await mediateur.EnvoyerAsync(new ObtenirAccesInstagramPublie(), arret);
            if (acces is null || !acces.DoitEtreRenouvele(horloge.Instant))
                return;

            await mediateur.EnvoyerAsync(new RenouvelerAccesInstagram(), arret);
            journal.LogInformation("Jeton Instagram prolongé de 60 jours.");
        }
        catch (Exception erreur) when (!arret.IsCancellationRequested)
        {
            journal.LogWarning(erreur, "Renouvellement du jeton Instagram impossible pour l'instant, nouvel essai plus tard.");
        }
    }
}
