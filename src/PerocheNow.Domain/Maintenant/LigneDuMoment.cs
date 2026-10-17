namespace PerocheNow.Domain.Maintenant;

/// <summary>
/// Une ligne complémentaire du statut du moment, par exemple « Sur Twitch, deux lives par semaine »,
/// et la rubrique du site où l'on en trouve le détail.
/// </summary>
public sealed class LigneDuMoment
{
    public int Ordre { get; private set; }
    public string Texte { get; private set; } = string.Empty;
    public Rubrique Rubrique { get; private set; }

    // Constructeur réservé à l'ORM.
    private LigneDuMoment() { }

    internal LigneDuMoment(int ordre, string texte, Rubrique rubrique)
    {
        Ordre = ordre;
        Texte = texte;
        Rubrique = rubrique;
    }
}
