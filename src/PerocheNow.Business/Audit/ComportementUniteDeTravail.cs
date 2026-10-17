using PerocheNow.Business.Mediateur;
using PerocheNow.Business.Ports;

namespace PerocheNow.Business.Audit;

/// <summary>
/// Enregistre en une seule fois tout ce qu'une commande a modifié, traces d'audit comprises.
/// Les gestionnaires ne sauvegardent jamais eux-mêmes : si quoi que ce soit échoue, rien n'est écrit.
/// </summary>
public sealed class ComportementUniteDeTravail<TRequete, TReponse>(IUniteDeTravail uniteDeTravail) : IComportement<TRequete, TReponse>
    where TRequete : IRequete<TReponse>
{
    public async Task<TReponse> TraiterAsync(TRequete requete, SuiteDuTraitement<TReponse> suite, CancellationToken annulation)
    {
        var reponse = await suite();

        if (requete is ICommande<TReponse>)
            await uniteDeTravail.EnregistrerAsync(annulation);

        return reponse;
    }
}
