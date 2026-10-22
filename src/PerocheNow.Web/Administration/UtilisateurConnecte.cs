using PerocheNow.Business.Ports;

namespace PerocheNow.Web.Administration;

/// <summary>
/// L'auteur des actions : le pseudo GitHub de la personne connectée à l'administration,
/// ou « système » hors requête (démarrage, initialisation de la base).
/// </summary>
public sealed class UtilisateurConnecte(IHttpContextAccessor acces) : IUtilisateurCourant
{
    public const string Systeme = "système";

    public string Nom =>
        acces.HttpContext?.User.Identity is { IsAuthenticated: true, Name: { Length: > 0 } nom } ? nom : Systeme;
}
