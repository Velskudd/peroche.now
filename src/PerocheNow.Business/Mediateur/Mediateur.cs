using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace PerocheNow.Business.Mediateur;

/// <summary>
/// Médiateur maison : retrouve le gestionnaire d'une requête dans le conteneur d'injection
/// et l'enveloppe dans les comportements transverses enregistrés.
/// </summary>
public sealed class Mediateur(IServiceProvider services) : IMediateur
{
    // Une enveloppe typée par type de requête, créée une seule fois puis réutilisée.
    private static readonly ConcurrentDictionary<Type, IEnveloppe> Enveloppes = new();

    public Task<TReponse> EnvoyerAsync<TReponse>(IRequete<TReponse> requete, CancellationToken annulation = default)
    {
        ArgumentNullException.ThrowIfNull(requete);

        var enveloppe = (IEnveloppe<TReponse>)Enveloppes.GetOrAdd(
            requete.GetType(),
            typeRequete => (IEnveloppe)Activator.CreateInstance(
                typeof(EnveloppeRequete<,>).MakeGenericType(typeRequete, typeof(TReponse)))!);

        return enveloppe.TraiterAsync(requete, services, annulation);
    }

    private interface IEnveloppe;

    private interface IEnveloppe<TReponse> : IEnveloppe
    {
        Task<TReponse> TraiterAsync(IRequete<TReponse> requete, IServiceProvider services, CancellationToken annulation);
    }

    private sealed class EnveloppeRequete<TRequete, TReponse> : IEnveloppe<TReponse>
        where TRequete : IRequete<TReponse>
    {
        public Task<TReponse> TraiterAsync(IRequete<TReponse> requete, IServiceProvider services, CancellationToken annulation)
        {
            var requeteTypee = (TRequete)requete;

            var gestionnaire = services.GetService<IGestionnaire<TRequete, TReponse>>()
                ?? throw new InvalidOperationException($"Aucun gestionnaire n'est enregistré pour {typeof(TRequete).Name}.");

            SuiteDuTraitement<TReponse> pipeline = () => gestionnaire.TraiterAsync(requeteTypee, annulation);

            // On enveloppe en partant du dernier comportement enregistré, pour que le premier soit le plus extérieur.
            foreach (var comportement in services.GetServices<IComportement<TRequete, TReponse>>().Reverse())
            {
                var suite = pipeline;
                pipeline = () => comportement.TraiterAsync(requeteTypee, suite, annulation);
            }

            return pipeline();
        }
    }
}
