using System.Globalization;
using PerocheNow.Business.Contenus;
using PerocheNow.Business.Mediateur;

namespace PerocheNow.Web.Contenus;

/// <summary>
/// Les derniers contenus pour l'accueil et la page Contenus. La page n'attend pas plus de trois secondes :
/// au-delà, elle s'affiche sans les contenus (le cache sera rempli pour la visite suivante).
/// </summary>
public sealed class LecteurDeContenus(IMediateur mediateur, ILogger<LecteurDeContenus> journal)
{
    private static readonly TimeSpan AttenteMaximale = TimeSpan.FromSeconds(3);
    private static readonly CultureInfo Francais = CultureInfo.GetCultureInfo("fr-FR");

    public async Task<IReadOnlyList<Contenu>> LireAsync(CancellationToken annulation = default)
    {
        using var delai = CancellationTokenSource.CreateLinkedTokenSource(annulation);
        delai.CancelAfter(AttenteMaximale);

        try
        {
            return await mediateur.EnvoyerAsync(new ObtenirDerniersContenus(), delai.Token);
        }
        catch (Exception erreur) when (!annulation.IsCancellationRequested)
        {
            journal.LogWarning(erreur, "Derniers contenus indisponibles, page affichée sans eux.");
            return [];
        }
    }

    /// <summary>« YouTube, 22 octobre 2026 ».</summary>
    public static string Mention(Contenu contenu) => $"{contenu.Plateforme}, {Date(contenu)}";

    /// <summary>« 22 octobre 2026 ».</summary>
    public static string Date(Contenu contenu) => contenu.PublieLe.ToString("d MMMM yyyy", Francais);
}
