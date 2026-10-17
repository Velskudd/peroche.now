using System.Diagnostics.CodeAnalysis;

namespace PerocheNow.Business.Mediateur;

/// <summary>Toute demande envoyée au médiateur, qui attend une réponse de type <typeparamref name="TReponse"/>.</summary>
[SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed",
    Justification = "Type fantôme : il permet au médiateur d'inférer le type de réponse de chaque requête.")]
public interface IRequete<TReponse>;

/// <summary>Demande en lecture seule. N'est jamais auditée.</summary>
public interface IConsultation<TReponse> : IRequete<TReponse>;

/// <summary>Demande qui modifie l'état de l'application. Chaque commande réussie est tracée par l'audit.</summary>
public interface ICommande<TReponse> : IRequete<TReponse>;

/// <summary>Commande sans valeur de retour.</summary>
public interface ICommande : ICommande<Unite>;

/// <summary>Équivalent de <c>void</c> pour les commandes qui ne renvoient rien.</summary>
public readonly record struct Unite
{
    public static readonly Unite Valeur = new();
}
