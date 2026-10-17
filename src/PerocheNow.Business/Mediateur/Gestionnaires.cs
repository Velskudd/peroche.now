namespace PerocheNow.Business.Mediateur;

/// <summary>Traite une requête précise. Un seul gestionnaire par type de requête.</summary>
public interface IGestionnaire<in TRequete, TReponse>
    where TRequete : IRequete<TReponse>
{
    Task<TReponse> TraiterAsync(TRequete requete, CancellationToken annulation);
}

/// <summary>Étape suivante du pipeline : le comportement suivant, ou le gestionnaire lui-même.</summary>
public delegate Task<TReponse> SuiteDuTraitement<TReponse>();

/// <summary>
/// Comportement transverse appliqué autour de chaque requête (audit, validation, journalisation…).
/// Les comportements s'exécutent dans leur ordre d'enregistrement, le premier enregistré étant le plus extérieur.
/// </summary>
public interface IComportement<in TRequete, TReponse>
    where TRequete : IRequete<TReponse>
{
    Task<TReponse> TraiterAsync(TRequete requete, SuiteDuTraitement<TReponse> suite, CancellationToken annulation);
}

/// <summary>Point d'entrée unique de l'IHM vers la couche Business.</summary>
public interface IMediateur
{
    Task<TReponse> EnvoyerAsync<TReponse>(IRequete<TReponse> requete, CancellationToken annulation = default);
}
