using System.Diagnostics.CodeAnalysis;

namespace PerocheNow.Business.Mediation;

/// <summary>Toute demande envoyée au médiateur, qui attend une réponse de type <typeparamref name="TResponse"/>.</summary>
[SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed",
    Justification = "Type fantôme : il permet au médiateur d'inférer le type de réponse de chaque requête.")]
public interface IRequest<TResponse>;

/// <summary>Demande en lecture seule (consultation). N'est jamais auditée.</summary>
public interface IQuery<TResponse> : IRequest<TResponse>;

/// <summary>Demande qui modifie l'état de l'application. Chaque commande réussie est tracée par l'audit.</summary>
public interface ICommand<TResponse> : IRequest<TResponse>;

/// <summary>Commande sans valeur de retour.</summary>
public interface ICommand : ICommand<Unit>;

/// <summary>Équivalent de <c>void</c> pour les commandes qui ne renvoient rien.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value = new();
}
