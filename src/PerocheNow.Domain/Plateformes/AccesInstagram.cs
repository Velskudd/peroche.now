using PerocheNow.Domain.Commun;

namespace PerocheNow.Domain.Plateformes;

/// <summary>
/// L'accès du site au compte Instagram : un jeton longue durée, valable 60 jours, que le site renouvelle
/// lui-même tant qu'il est encore valide. Il n'en existe qu'un.
/// </summary>
public sealed class AccesInstagram : Entite
{
    public const int LongueurMaxJeton = 1000;
    public const int LongueurMaxCompte = 100;

    /// <summary>Durée de vie d'un jeton longue durée Instagram.</summary>
    public static readonly TimeSpan DureeDuJeton = TimeSpan.FromDays(60);

    /// <summary>
    /// Âge à partir duquel on renouvelle le jeton. Instagram refuse de renouveler un jeton de moins de 24 h ;
    /// le renouveler chaque semaine laisse plus de sept semaines de marge si le site reste en veille.
    /// </summary>
    public static readonly TimeSpan AgeDeRenouvellement = TimeSpan.FromDays(7);

    public string Jeton { get; private set; } = string.Empty;
    public string Compte { get; private set; } = string.Empty;
    public DateTimeOffset ObtenuLe { get; private set; }
    public DateTimeOffset ExpireLe { get; private set; }

    // Constructeur réservé à l'ORM.
    private AccesInstagram() { }

    public static AccesInstagram Creer(string jeton, string compte, DateTimeOffset expireLe, DateTimeOffset maintenant)
    {
        var acces = new AccesInstagram();
        acces.Remplacer(jeton, compte, expireLe, maintenant);
        return acces;
    }

    /// <summary>Un nouveau jeton, saisi dans l'administration, remplace l'ancien.</summary>
    public void Remplacer(string jeton, string compte, DateTimeOffset expireLe, DateTimeOffset maintenant)
    {
        if (string.IsNullOrWhiteSpace(compte))
            throw new RegleMetierException("Le compte Instagram est obligatoire.");

        var nettoye = compte.Trim();
        if (nettoye.Length > LongueurMaxCompte)
            throw new RegleMetierException($"Le nom du compte Instagram ne doit pas dépasser {LongueurMaxCompte} caractères.");

        Compte = nettoye;
        Renouveler(jeton, expireLe, maintenant);
    }

    /// <summary>Le jeton prolongé par Instagram remplace l'ancien, pour le même compte.</summary>
    public void Renouveler(string jeton, DateTimeOffset expireLe, DateTimeOffset maintenant)
    {
        if (string.IsNullOrWhiteSpace(jeton))
            throw new RegleMetierException("Le jeton Instagram est obligatoire.");

        var nettoye = jeton.Trim();
        if (nettoye.Length > LongueurMaxJeton || nettoye.Any(char.IsWhiteSpace))
            throw new RegleMetierException("Ce jeton Instagram n'a pas la bonne forme : il doit tenir en un seul mot.");

        if (expireLe <= maintenant)
            throw new RegleMetierException("Ce jeton Instagram est déjà expiré.");

        Jeton = nettoye;
        ObtenuLe = maintenant;
        ExpireLe = expireLe;
    }

    public bool EstExpire(DateTimeOffset maintenant) => maintenant >= ExpireLe;

    public bool DoitEtreRenouvele(DateTimeOffset maintenant) =>
        !EstExpire(maintenant) && maintenant - ObtenuLe >= AgeDeRenouvellement;
}
