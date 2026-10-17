using PerocheNow.Domain.Commun;

namespace PerocheNow.Domain.Maintenant;

/// <summary>
/// Le prochain objectif affiché en or. La date peut rester inconnue tant qu'elle n'est pas confirmée.
/// </summary>
public sealed record Objectif
{
    public const int LongueurMaxLibelle = 80;

    public string Libelle { get; }
    public DateOnly? DateCible { get; }

    public Objectif(string libelle, DateOnly? dateCible = null)
    {
        if (string.IsNullOrWhiteSpace(libelle))
            throw new RegleMetierException("Le libellé de l'objectif est obligatoire.");

        var nettoye = libelle.Trim();
        if (nettoye.Length > LongueurMaxLibelle)
            throw new RegleMetierException($"Le libellé de l'objectif ne doit pas dépasser {LongueurMaxLibelle} caractères.");

        Libelle = nettoye;
        DateCible = dateCible;
    }

    public bool DateConfirmee => DateCible.HasValue;
}
