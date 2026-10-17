namespace PerocheNow.Domain.Commun;

/// <summary>
/// Levée quand une règle métier du domaine n'est pas respectée.
/// </summary>
public sealed class RegleMetierException(string message) : Exception(message);
