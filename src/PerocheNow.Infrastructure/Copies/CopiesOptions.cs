namespace PerocheNow.Infrastructure.Copies;

/// <summary>
/// Section <c>Copies</c> de la configuration : le conteneur Azure Blob qui garde les copies de lecture.
/// Tant qu'un réglage manque, il n'y a pas de copie et le site lit la base directement (comme en local).
/// </summary>
public sealed class CopiesOptions
{
    public const string Section = "Copies";

    /// <summary>Adresse du conteneur, par exemple <c>https://compte.blob.core.windows.net/copies</c>.</summary>
    public Uri? AdresseConteneur { get; init; }

    /// <summary>Nom du compte de stockage.</summary>
    public string? Compte { get; init; }

    /// <summary>Clé d'accès du compte (en base 64), fournie par l'infrastructure comme secret.</summary>
    public string? Cle { get; init; }

    /// <summary>Les paramètres complets, ou <c>null</c> tant qu'il manque un réglage.</summary>
    public ParametresCopies? Parametres() =>
        AdresseConteneur is null || string.IsNullOrWhiteSpace(Compte) || string.IsNullOrWhiteSpace(Cle)
            ? null
            : new ParametresCopies(AdresseConteneur, Compte, Convert.FromBase64String(Cle));
}

/// <summary>Configuration du conteneur de copies, complète et validée.</summary>
public sealed record ParametresCopies(Uri AdresseConteneur, string Compte, byte[] Cle)
{
    /// <summary>Jamais la clé dans un journal.</summary>
    public override string ToString() => $"Copies de lecture ({AdresseConteneur})";
}
