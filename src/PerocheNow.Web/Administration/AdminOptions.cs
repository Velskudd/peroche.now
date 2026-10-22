namespace PerocheNow.Web.Administration;

/// <summary>
/// Section <c>Admin</c> de la configuration : qui peut administrer le site, et l'application GitHub
/// qui sert à se connecter. Tant que l'application GitHub n'est pas renseignée, l'administration est fermée.
/// </summary>
public sealed class AdminOptions
{
    public const string Section = "Admin";

    /// <summary>
    /// Identifiants numériques GitHub autorisés. On compare l'identifiant et non le pseudo,
    /// car un pseudo peut changer de propriétaire.
    /// </summary>
    public IReadOnlyList<string> Administrateurs { get; init; } = [];

    public ConnexionGitHubOptions GitHub { get; init; } = new();
}

/// <summary>L'application OAuth déclarée sur GitHub (Settings › Developer settings › OAuth Apps).</summary>
public sealed class ConnexionGitHubOptions
{
    public string? IdentifiantClient { get; init; }
    public string? SecretClient { get; init; }
    public Uri? AdresseAutorisation { get; init; }
    public Uri? AdresseJeton { get; init; }
    public Uri? AdresseUtilisateur { get; init; }

    public bool EstConfiguree =>
        !string.IsNullOrWhiteSpace(IdentifiantClient)
        && !string.IsNullOrWhiteSpace(SecretClient)
        && AdresseAutorisation is not null
        && AdresseJeton is not null
        && AdresseUtilisateur is not null;
}
