using PerocheNow.Business.Mediateur;

namespace PerocheNow.Business.Audit;

/// <summary>
/// Trace automatiquement chaque commande réussie. Les consultations passent sans être tracées.
/// Si la réponse de la commande est un identifiant, il est rattaché à la trace.
/// La trace est enregistrée avec la commande, dans la même transaction (voir <see cref="ComportementUniteDeTravail{TRequete, TReponse}"/>).
/// </summary>
public sealed class ComportementAudit<TRequete, TReponse>(IAuditService audit) : IComportement<TRequete, TReponse>
    where TRequete : IRequete<TReponse>
{
    public async Task<TReponse> TraiterAsync(TRequete requete, SuiteDuTraitement<TReponse> suite, CancellationToken annulation)
    {
        var reponse = await suite();

        if (requete is ICommande<TReponse>)
        {
            audit.Tracer(
                action: typeof(TRequete).Name,
                idObjet: reponse is Guid id ? id : null);
        }

        return reponse;
    }
}
