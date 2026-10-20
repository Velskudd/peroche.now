namespace PerocheNow.Domain.Status;

/// <summary>
/// Une ligne complémentaire du statut du moment, par exemple « Sur Twitch, deux lives par semaine »,
/// et la rubrique du site où l'on en trouve le détail.
/// </summary>
public sealed class StatusLine
{
    public int Position { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public Section Section { get; private set; }

    // Constructeur réservé à l'ORM.
    private StatusLine() { }

    internal StatusLine(int position, string text, Section section)
    {
        Position = position;
        Text = text;
        Section = section;
    }
}
