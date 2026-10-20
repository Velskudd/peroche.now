using PerocheNow.Domain.Common;

namespace PerocheNow.Domain.Status;

/// <summary>
/// Ce qu'Alexandre fait en ce moment : la phrase d'accroche de l'accueil,
/// jusqu'à trois lignes complémentaires et le prochain objectif.
/// </summary>
public sealed class CurrentStatus : Entity
{
    public const int MaxHeadlineLength = 140;
    public const int MaxLineCount = 3;
    public const int MaxLineLength = 200;

    private readonly List<StatusLine> _lines = [];

    public string Headline { get; private set; } = string.Empty;
    /// <summary>Les lignes complémentaires. Utiliser <see cref="StatusLine.Position"/> pour l'affichage.</summary>
    public IReadOnlyCollection<StatusLine> Lines => _lines.AsReadOnly();
    public Goal? NextGoal { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Constructeur réservé à l'ORM.
    private CurrentStatus() { }

    public static CurrentStatus Create(string headline, DateTimeOffset now)
    {
        var status = new CurrentStatus();
        status.ChangeHeadline(headline, now);
        return status;
    }

    public void ChangeHeadline(string headline, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(headline))
            throw new BusinessRuleException("L'accroche du moment est obligatoire.");

        var trimmed = headline.Trim();
        if (trimmed.Length > MaxHeadlineLength)
            throw new BusinessRuleException($"L'accroche ne doit pas dépasser {MaxHeadlineLength} caractères.");

        Headline = trimmed;
        UpdatedAt = now;
    }

    /// <summary>
    /// Remplace toutes les lignes complémentaires, sans rubrique. L'ordre de la liste devient l'ordre d'affichage.
    /// </summary>
    public void ReplaceLines(IEnumerable<string> texts, DateTimeOffset now)
        => ReplaceLines(texts.Select(t => (t, Section.None)), now);

    /// <summary>
    /// Remplace toutes les lignes complémentaires, chacune avec la rubrique vers laquelle elle mène.
    /// L'ordre de la liste devient l'ordre d'affichage.
    /// </summary>
    public void ReplaceLines(IEnumerable<(string Text, Section Section)> lines, DateTimeOffset now)
    {
        var trimmed = lines
            .Where(l => !string.IsNullOrWhiteSpace(l.Text))
            .Select(l => (Text: l.Text.Trim(), l.Section))
            .ToList();

        if (trimmed.Count > MaxLineCount)
            throw new BusinessRuleException($"Pas plus de {MaxLineCount} lignes : l'accueil doit rester lisible d'un coup d'œil.");

        if (trimmed.Any(l => l.Text.Length > MaxLineLength))
            throw new BusinessRuleException($"Une ligne ne doit pas dépasser {MaxLineLength} caractères.");

        if (trimmed.Any(l => !Enum.IsDefined(l.Section)))
            throw new BusinessRuleException("Une ligne mène vers une rubrique inconnue.");

        _lines.Clear();
        for (var i = 0; i < trimmed.Count; i++)
            _lines.Add(new StatusLine(i + 1, trimmed[i].Text, trimmed[i].Section));

        UpdatedAt = now;
    }

    public void SetNextGoal(Goal? goal, DateTimeOffset now)
    {
        NextGoal = goal;
        UpdatedAt = now;
    }
}
