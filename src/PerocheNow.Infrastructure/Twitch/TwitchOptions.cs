namespace PerocheNow.Infrastructure.Twitch;

/// <summary>
/// Section <c>Twitch</c> de la configuration. L'identifiant et le secret viennent de l'application
/// déclarée sur la console développeur de Twitch ; tant qu'ils manquent, la pastille « En direct » reste masquée.
/// </summary>
public sealed class TwitchOptions
{
    public const string Section = "Twitch";

    /// <summary>Le nom de la chaîne, tel qu'il apparaît dans son adresse (ap15fr).</summary>
    public string? Chaine { get; init; }

    public string? IdentifiantClient { get; init; }
    public string? SecretClient { get; init; }

    /// <summary>Point d'accès de l'API Helix qui liste les directs en cours.</summary>
    public Uri? AdresseFlux { get; init; }

    /// <summary>Point d'accès qui délivre les jetons d'application.</summary>
    public Uri? AdresseJeton { get; init; }

    /// <summary>Les paramètres complets, ou <c>null</c> tant qu'il manque un réglage.</summary>
    public ParametresTwitch? Parametres() =>
        string.IsNullOrWhiteSpace(Chaine)
        || string.IsNullOrWhiteSpace(IdentifiantClient)
        || string.IsNullOrWhiteSpace(SecretClient)
        || AdresseFlux is null
        || AdresseJeton is null
            ? null
            : new ParametresTwitch(Chaine, IdentifiantClient, SecretClient, AdresseFlux, AdresseJeton);
}

/// <summary>Configuration Twitch complète et validée.</summary>
public sealed record ParametresTwitch(
    string Chaine,
    string IdentifiantClient,
    string SecretClient,
    Uri AdresseFlux,
    Uri AdresseJeton)
{
    /// <summary>Jamais le secret dans un journal.</summary>
    public override string ToString() => $"Twitch ({Chaine})";
}
