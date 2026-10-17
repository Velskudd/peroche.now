using PerocheNow.Infrastructure.Persistance;

namespace PerocheNow.Web.Demarrage;

/// <summary>
/// Prépare la base en arrière-plan, sans retarder le démarrage du site :
/// une base serverless en pause peut mettre plusieurs dizaines de secondes à se réveiller.
/// En cas d'échec, le site tourne quand même avec le statut de secours ; on retentera au prochain démarrage.
/// </summary>
public sealed class InitialisationAuDemarrage(IServiceProvider services, ILogger<InitialisationAuDemarrage> journal)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken arret)
    {
        try
        {
            await services.InitialiserBaseAsync(arret);
            journal.LogInformation("Base prête.");
        }
        catch (OperationCanceledException) when (arret.IsCancellationRequested)
        {
        }
        catch (Exception erreur)
        {
            journal.LogError(erreur, "La base n'a pas pu être préparée. Le site affichera le statut de secours.");
        }
    }
}
