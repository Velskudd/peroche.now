using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace PerocheNow.Business.Mediation;

/// <summary>
/// Médiateur maison : retrouve le gestionnaire d'une requête dans le conteneur d'injection
/// et l'enveloppe dans les comportements transverses enregistrés.
/// </summary>
public sealed class Mediator(IServiceProvider services) : IMediator
{
    // Une enveloppe typée par type de requête, créée une seule fois puis réutilisée.
    private static readonly ConcurrentDictionary<Type, IWrapper> Wrappers = new();

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (IWrapper<TResponse>)Wrappers.GetOrAdd(
            request.GetType(),
            requestType => (IWrapper)Activator.CreateInstance(
                typeof(RequestWrapper<,>).MakeGenericType(requestType, typeof(TResponse)))!);

        return wrapper.HandleAsync(request, services, cancellationToken);
    }

    private interface IWrapper;

    private interface IWrapper<TResponse> : IWrapper
    {
        Task<TResponse> HandleAsync(IRequest<TResponse> request, IServiceProvider services, CancellationToken cancellationToken);
    }

    private sealed class RequestWrapper<TRequest, TResponse> : IWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> HandleAsync(IRequest<TResponse> request, IServiceProvider services, CancellationToken cancellationToken)
        {
            var typedRequest = (TRequest)request;

            var handler = services.GetService<IHandler<TRequest, TResponse>>()
                ?? throw new InvalidOperationException($"Aucun gestionnaire n'est enregistré pour {typeof(TRequest).Name}.");

            NextStep<TResponse> pipeline = () => handler.HandleAsync(typedRequest, cancellationToken);

            // On enveloppe en partant du dernier comportement enregistré, pour que le premier soit le plus extérieur.
            foreach (var behavior in services.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse())
            {
                var next = pipeline;
                pipeline = () => behavior.HandleAsync(typedRequest, next, cancellationToken);
            }

            return pipeline();
        }
    }
}
