namespace PerocheNow.Domain.Common;

/// <summary>
/// Levée quand une règle métier du domaine n'est pas respectée.
/// </summary>
public sealed class BusinessRuleException(string message) : Exception(message);
