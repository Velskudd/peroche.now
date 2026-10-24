namespace PerocheNow.Web.Administration;

/// <summary>Les messages de l'administration quand quelque chose ne répond pas, écrits une seule fois.</summary>
public static class MessagesAdmin
{
    public const string BaseInjoignable =
        "La base de données ne répond pas. Elle est peut-être en pause (quota gratuit du mois épuisé) : réessaie plus tard.";

    public const string BranchementImpossible =
        "Le branchement n'a pas abouti : Instagram ou la base de données ne répond pas. Réessaie plus tard.";

    public const string CopieNonMiseAJour =
        "C'est enregistré, mais le site public n'a pas pu être mis à jour. Enregistre à nouveau dans un instant.";
}
