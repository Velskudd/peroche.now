using PerocheNow.Domain.Common;

namespace PerocheNow.Domain.Status;

/// <summary>
/// Le prochain objectif affiché en or. La date peut rester inconnue tant qu'elle n'est pas confirmée.
/// </summary>
public sealed record Goal
{
    public const int MaxLabelLength = 80;

    public string Label { get; }
    public DateOnly? TargetDate { get; }

    public Goal(string label, DateOnly? targetDate = null)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new BusinessRuleException("Le libellé de l'objectif est obligatoire.");

        var trimmed = label.Trim();
        if (trimmed.Length > MaxLabelLength)
            throw new BusinessRuleException($"Le libellé de l'objectif ne doit pas dépasser {MaxLabelLength} caractères.");

        Label = trimmed;
        TargetDate = targetDate;
    }

    public bool IsDateConfirmed => TargetDate.HasValue;
}
