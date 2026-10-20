using PerocheNow.Business.Mediation;
using PerocheNow.Business.Ports;

namespace PerocheNow.Business.Audit;

/// <summary>
/// Enregistre en une seule fois tout ce qu'une commande a modifié, traces d'audit comprises.
/// Les gestionnaires ne sauvegardent jamais eux-mêmes : si quoi que ce soit échoue, rien n'est écrit.
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, NextStep<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is ICommand<TResponse>)
            await unitOfWork.SaveAsync(cancellationToken);

        return response;
    }
}
