using PerocheNow.Business.Mediation;

namespace PerocheNow.Business.Audit;

/// <summary>
/// Trace automatiquement chaque commande réussie. Les consultations passent sans être tracées.
/// Si la réponse de la commande est un identifiant, il est rattaché à la trace.
/// La trace est enregistrée avec la commande, dans la même transaction (voir <see cref="UnitOfWorkBehavior{TRequest, TResponse}"/>).
/// </summary>
public sealed class AuditBehavior<TRequest, TResponse>(IAuditService audit) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, NextStep<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is ICommand<TResponse>)
        {
            audit.Record(
                action: typeof(TRequest).Name,
                objectId: response is Guid id ? id : null);
        }

        return response;
    }
}
