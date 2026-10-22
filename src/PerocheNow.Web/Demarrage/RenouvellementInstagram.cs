using PerocheNow.Business.Contenus.Instagram;
using PerocheNow.Business.Mediateur;

namespace PerocheNow.Web.Demarrage;

/// <summary>
/// Prolonge le jeton Instagram quand il a plus d'une semaine : peu après chaque démarrage du site
/// (il se réveille à chaque visite), puis toutes les douze heures tant qu'il tourne.
/// Rien n'est tracé quand il n'y a rien à faire.
/// </summary>
public sealed class RenouvellementInstagram(IServiceScopeFactory portees, ILogger<RenouvellementInstagram> journal)
    : BackgroundService
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

            var etat = await mediateur.EnvoyerAsync(new ObtenirEtatInstagram(), arret);
            if (etat is not { DoitEtreRenouvele: true })
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
