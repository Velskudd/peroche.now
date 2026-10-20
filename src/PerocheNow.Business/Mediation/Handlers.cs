namespace PerocheNow.Business.Mediation;

/// <summary>Traite une requête précise. Un seul gestionnaire par type de requête.</summary>
public interface IHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

/// <summary>Étape suivante du pipeline : le comportement suivant, ou le gestionnaire lui-même.</summary>
public delegate Task<TResponse> NextStep<TResponse>();

/// <summary>
/// Comportement transverse appliqué autour de chaque requête (audit, validation, journalisation…).
/// Les comportements s'exécutent dans leur ordre d'enregistrement, le premier enregistré étant le plus extérieur.
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, NextStep<TResponse> next, CancellationToken cancellationToken);
}

/// <summary>Point d'entrée unique de l'IHM vers la couche Business.</summary>
public interface IMediator
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
