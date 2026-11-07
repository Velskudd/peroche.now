using Microsoft.AspNetCore.Mvc;
using PerocheNow.Business.Audit;
using PerocheNow.Business.Medias;
using PerocheNow.Business.Mediateur;
using PerocheNow.Business.Ports;
using PerocheNow.Business.Textes;
using PerocheNow.Domain.Commun;
using PerocheNow.Domain.Medias;
using PerocheNow.Web.Administration;
using PerocheNow.Web.Textes;

namespace PerocheNow.Web.Medias;

/// <summary>
/// Les fichiers des médias, servis aux visiteurs depuis le stockage (<c>/medias/…</c>),
/// et les envois depuis l'administration (<c>/admin/medias/…</c>).
/// </summary>
public static class PointsDAccesMedias
{
    public const string AdresseAdmin = "/admin/medias";

    /// <summary>Un envoi un peu plus gros que la plus grosse vidéo acceptée, pour l'enveloppe du formulaire.</summary>
    private const long TailleMaxEnvoi = RegleDesMedias.TailleMaxVideo + 1024 * 1024;

    public static IEndpointRouteBuilder MapMedias(this IEndpointRouteBuilder app)
    {
        app.MapGet(MediasDuSite.Prefixe + "{fichier}", ServirAsync);

        var admin = app.MapGroup(AdresseAdmin).RequireAuthorization(EnregistrementAdministration.PolitiqueAdministrateur);
        admin.MapPost("/deposer", DeposerAsync)
            .WithMetadata(new RequestSizeLimitAttribute(TailleMaxEnvoi))
            .WithFormOptions(multipartBodyLengthLimit: TailleMaxEnvoi);
        admin.MapPost("/retirer", RetirerAsync);
        return app;
    }

    /// <summary>
    /// Sert un fichier rangé par le site, en entier ou par plage (les vidéos se lisent par morceaux).
    /// Son nom change à chaque envoi : il peut rester en cache un an.
    /// </summary>
    private static async Task ServirAsync(string fichier, HttpContext contexte, IStockageDeMedias stockage, CancellationToken annulation)
    {
        var reponse = contexte.Response;
        if (!RegleDesMedias.EstUnNomDeFichier(fichier))
        {
            reponse.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        string? plage = contexte.Request.Headers.Range;
        if (plage is not null && (!plage.StartsWith("bytes=", StringComparison.Ordinal) || plage.Contains(',', StringComparison.Ordinal)))
            plage = null;

        await using var media = await stockage.OuvrirAsync(fichier, plage, annulation);
        if (media is null)
        {
            reponse.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        reponse.StatusCode = media.EstPartiel ? StatusCodes.Status206PartialContent : StatusCodes.Status200OK;
        reponse.ContentType = media.TypeContenu;
        reponse.ContentLength = media.Longueur;
        reponse.Headers.AcceptRanges = "bytes";
        reponse.Headers.CacheControl = "public, max-age=31536000, immutable";
        if (media.PlageRenvoyee is not null)
            reponse.Headers.ContentRange = media.PlageRenvoyee;
        if (media.Empreinte is not null)
            reponse.Headers.ETag = media.Empreinte;

        await media.Contenu.CopyToAsync(reponse.Body, annulation);
    }

    private static async Task<IResult> DeposerAsync(
        [FromForm] string cle,
        [FromForm] string? description,
        IFormFile? fichier,
        IMediateur mediateur,
        TextesDuSite textes,
        MediasDuSite medias,
        ILogger<MediasDuSite> journal,
        CancellationToken annulation)
    {
        if (CatalogueDesMedias.Trouver(cle) is not { } emplacement)
            return Results.BadRequest();

        try
        {
            if (fichier is { Length: > 0 })
            {
                var tailleMax = emplacement.Genre == GenreDeMedia.Image ? RegleDesMedias.TailleMaxImage : RegleDesMedias.TailleMaxVideo;
                if (fichier.Length > tailleMax)
                    return Retour(cle, EchecDEnvoi.Taille);

                using var memoire = new MemoryStream((int)fichier.Length);
                await fichier.CopyToAsync(memoire, annulation);
                await mediateur.EnvoyerAsync(new DeposerMedia(cle, emplacement.Genre, memoire.GetBuffer().AsMemory(0, (int)memoire.Length)), annulation);
            }

            if (emplacement.Description is { } definition && description is not null)
            {
                var saisie = description.Trim();
                var origine = saisie.Length == 0 || string.Equals(saisie, definition.ParDefaut, StringComparison.Ordinal);
                await mediateur.EnvoyerAsync(new ModifierTextesDuSite([new TexteSaisi(definition.Cle, origine ? null : saisie)]), annulation);
            }
        }
        catch (RegleMetierException)
        {
            // La taille et l'emplacement sont vérifiés plus haut : il reste le format du fichier.
            return Retour(cle, EchecDEnvoi.Format);
        }
        catch (PublicationIncompleteException incomplete)
        {
            journal.LogWarning(incomplete, "Média enregistré, copie de lecture non mise à jour.");
            return Retour(cle, EchecDEnvoi.Copie);
        }
        catch (Exception panne) when (!annulation.IsCancellationRequested)
        {
            journal.LogWarning(panne, "Envoi du média {Emplacement} impossible.", cle);
            return Retour(cle, EchecDEnvoi.Panne);
        }

        textes.Oublier();
        medias.Oublier();
        return Retour(cle);
    }

    private static async Task<IResult> RetirerAsync(
        [FromForm] string cle,
        IMediateur mediateur,
        TextesDuSite textes,
        MediasDuSite medias,
        ILogger<MediasDuSite> journal,
        CancellationToken annulation)
    {
        if (CatalogueDesMedias.Trouver(cle) is not { } emplacement)
            return Results.BadRequest();

        try
        {
            await mediateur.EnvoyerAsync(new RetirerMedia(cle), annulation);

            // La description parlait de la photo retirée : elle revient à celle de la photo d'origine.
            if (emplacement.Description is { } definition)
                await mediateur.EnvoyerAsync(new ModifierTextesDuSite([new TexteSaisi(definition.Cle, null)]), annulation);
        }
        catch (PublicationIncompleteException incomplete)
        {
            journal.LogWarning(incomplete, "Média retiré, copie de lecture non mise à jour.");
            return Retour(cle, EchecDEnvoi.Copie);
        }
        catch (Exception panne) when (!annulation.IsCancellationRequested)
        {
            journal.LogWarning(panne, "Retrait du média {Emplacement} impossible.", cle);
            return Retour(cle, EchecDEnvoi.Panne);
        }

        textes.Oublier();
        medias.Oublier();
        return Retour(cle);
    }

    /// <summary>
    /// Retour à la page des médias, sur l'emplacement concerné, avec le résultat.
    /// L'adresse ne porte qu'un code : la page choisit elle-même le message à afficher.
    /// </summary>
    private static IResult Retour(string cle, EchecDEnvoi? echec = null)
    {
        var requete = $"?emplacement={Uri.EscapeDataString(cle)}"
            + (echec is null ? "&resultat=ok" : $"&resultat={echec.Value.ToString().ToLowerInvariant()}");
        return Results.LocalRedirect($"{AdresseAdmin}{requete}#{Ancre(cle)}");
    }

    public static string Ancre(string cle) => "media-" + cle.Replace('.', '-');
}

/// <summary>Pourquoi un envoi n'a pas abouti.</summary>
public enum EchecDEnvoi
{
    Taille,
    Format,
    Copie,
    Panne,
}
