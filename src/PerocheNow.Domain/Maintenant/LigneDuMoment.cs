namespace PerocheNow.Domain.Maintenant;

/// <summary>
/// Une ligne complémentaire du statut du moment, par exemple « Sur Twitch, deux lives par semaine ».
/// </summary>
public sealed class LigneDuMoment
{
    public int Ordre { get; private set; }
    public string Texte { get; private set; } = string.Empty;

    // Constructeur réservé à l'ORM.
    private LigneDuMoment() { }

    internal LigneDuMoment(int ordre, string texte)
    {
        Ordre = ordre;
        Texte = texte;
    }
}
