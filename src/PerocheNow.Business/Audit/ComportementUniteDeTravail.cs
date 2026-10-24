using PerocheNow.Business.Mediateur;
using PerocheNow.Business.Ports;

namespace PerocheNow.Business.Audit;

/// <summary>
/// Enregistre en une seule fois tout ce qu'une commande a modifié, traces d'audit comprises,
/// puis lance ce que la commande a prévu pour après (<see cref="ActionsApresEnregistrement"/>).
/// Les gestionnaires ne sauvegardent jamais eux-mêmes : si quoi que ce soit échoue, rien n'est écrit.
/// </summary>
public sealed class ComportementUniteDeTravail<TRequete, TReponse>(
    IUniteDeTravail uniteDeTravail,
    ActionsApresEnregistrement apresEnregistrement) : IComportement<TRequete, TReponse>
    where TRequete : IRequete<TReponse>
{
    public async Task<TReponse> TraiterAsync(TRequete requete, SuiteDuTraitement<TReponse> suite, CancellationToken annulation)
    {
        if (requete is not ICommande<TReponse>)
            return await suite();

        TReponse reponse;
        try
        {
            reponse = await suite();
            await uniteDeTravail.EnregistrerAsync(annulation);
        }
        catch
        {
            apresEnregistrement.Abandonner();
            throw;
        }

        await apresEnregistrement.ExecuterAsync(annulation);
        return reponse;
    }
}
