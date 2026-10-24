namespace PerocheNow.Business.CopiesDeLecture;

/// <summary>Les copies de lecture publiées par le site, une par sujet.</summary>
public static class NomsDesCopies
{
    /// <summary>Ce que l'accueil et la page de liens affichent en premier.</summary>
    public const string StatutDuMoment = "statut-du-moment.json";

    /// <summary>L'accès au compte Instagram (jeton compris) : la copie n'est jamais publique.</summary>
    public const string AccesInstagram = "acces-instagram.json";

    /// <summary>La dernière migration appliquée : évite de réveiller la base à chaque démarrage du site.</summary>
    public const string EtatDeLaBase = "etat-de-la-base.json";
}
