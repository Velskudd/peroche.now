using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace PerocheNow.Infrastructure.Copies;

/// <summary>
/// Signature « clé partagée » des requêtes Azure Storage (en-tête <c>Authorization: SharedKey compte:signature</c>).
/// Écrite à la main pour rester sur <see cref="HttpClient"/>, comme les autres adaptateurs du site.
/// Référence : https://learn.microsoft.com/rest/api/storageservices/authorize-with-shared-key
/// </summary>
public static class SignatureCleePartagee
{
    /// <summary>Version de l'API REST Blob demandée.</summary>
    public const string VersionApi = "2023-11-03";

    private const string EnTeteDate = "x-ms-date";
    private const string EnTeteVersion = "x-ms-version";
    private const string PrefixeAzure = "x-ms-";

    /// <summary>Date la requête, fixe la version d'API, puis ajoute la signature.</summary>
    public static void Signer(HttpRequestMessage requete, string compte, byte[] cle, DateTimeOffset instant)
    {
        requete.Headers.Remove(EnTeteDate);
        requete.Headers.Remove(EnTeteVersion);
        requete.Headers.TryAddWithoutValidation(EnTeteDate, instant.UtcDateTime.ToString("R", CultureInfo.InvariantCulture));
        requete.Headers.TryAddWithoutValidation(EnTeteVersion, VersionApi);

        using var hmac = new HMACSHA256(cle);
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(ChaineASigner(requete, compte))));
        requete.Headers.Authorization = new AuthenticationHeaderValue("SharedKey", $"{compte}:{signature}");
    }

    /// <summary>
    /// La chaîne signée, dans l'ordre imposé par Azure. Le site n'envoie ni en-tête <c>Date</c> (remplacé par
    /// <c>x-ms-date</c>), ni condition (<c>If-*</c>), ni plage (<c>Range</c>) : ces lignes restent vides.
    /// </summary>
    public static string ChaineASigner(HttpRequestMessage requete, string compte)
    {
        var contenu = requete.Content?.Headers;
        // Depuis la version 2015-02-21 de l'API, une longueur nulle s'écrit comme une ligne vide.
        var longueur = contenu?.ContentLength is long octets and > 0
            ? octets.ToString(CultureInfo.InvariantCulture)
            : string.Empty;

        string[] lignes =
        [
            requete.Method.Method,
            contenu is null ? string.Empty : string.Join(',', contenu.ContentEncoding),
            contenu is null ? string.Empty : string.Join(',', contenu.ContentLanguage),
            longueur,
            contenu?.ContentMD5 is { } empreinte ? Convert.ToBase64String(empreinte) : string.Empty,
            contenu?.ContentType?.ToString() ?? string.Empty,
            string.Empty, // Date
            string.Empty, // If-Modified-Since
            string.Empty, // If-Match
            string.Empty, // If-None-Match
            string.Empty, // If-Unmodified-Since
            string.Empty, // Range
        ];

        return string.Join('\n', lignes) + '\n' + EnTetesCanoniques(requete) + RessourceCanonique(requete.RequestUri!, compte);
    }

    private static string EnTetesCanoniques(HttpRequestMessage requete) =>
        string.Concat(requete.Headers
            .Where(entete => entete.Key.StartsWith(PrefixeAzure, StringComparison.OrdinalIgnoreCase))
            .Select(entete => (Nom: entete.Key.ToLowerInvariant(), Valeur: string.Join(',', entete.Value).Trim()))
            .OrderBy(entete => entete.Nom, StringComparer.Ordinal)
            .Select(entete => $"{entete.Nom}:{entete.Valeur}\n"));

    /// <summary>
    /// <c>/compte/chemin</c>, puis chaque paramètre de la requête (nom en minuscules, trié), un par ligne.
    /// Avec l'émulateur (adresse de la forme <c>/compte/conteneur</c>), le nom du compte apparaît donc deux fois.
    /// </summary>
    private static string RessourceCanonique(Uri adresse, string compte)
    {
        var ressource = new StringBuilder("/").Append(compte).Append(adresse.AbsolutePath);

        var parametres = adresse.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(paire => paire.Split('=', 2))
            .GroupBy(paire => Uri.UnescapeDataString(paire[0]).ToLowerInvariant(), StringComparer.Ordinal)
            .OrderBy(groupe => groupe.Key, StringComparer.Ordinal);

        foreach (var groupe in parametres)
        {
            var valeurs = groupe
                .Select(paire => paire.Length > 1 ? Uri.UnescapeDataString(paire[1]) : string.Empty)
                .OrderBy(valeur => valeur, StringComparer.Ordinal);
            ressource.Append('\n').Append(groupe.Key).Append(':').Append(string.Join(',', valeurs));
        }

        return ressource.ToString();
    }
}
