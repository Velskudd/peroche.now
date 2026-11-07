using PerocheNow.Business.Audit;
using PerocheNow.Business.CopiesDeLecture;
using PerocheNow.Business.Mediateur;
using PerocheNow.Business.Ports;
using PerocheNow.Domain.Medias;

namespace PerocheNow.Business.Medias;

/// <summary>
/// Remplace la photo ou la vidéo d'un emplacement du site par un fichier envoyé depuis l'administration.
/// Le fichier est vérifié, rangé dans le stockage, puis noté en base. Renvoie le nom du fichier rangé.
/// </summary>
public sealed record DeposerMedia(string Cle, GenreDeMedia Genre, ReadOnlyMemory<byte> Contenu) : ICommande<string>
{
    /// <summary>Jamais le contenu dans un journal.</summary>
    public override string ToString() => $"{nameof(DeposerMedia)} {Cle} ({Contenu.Length} octets)";
}

public sealed class DeposerMediaGestionnaire(
    IMediasDuSiteRepository medias,
    IStockageDeMedias stockage,
    IHorloge horloge,
    ICopiesDeLecture copies,
    ActionsApresEnregistrement apresEnregistrement) : IGestionnaire<DeposerMedia, string>
{
    public async Task<string> TraiterAsync(DeposerMedia requete, CancellationToken annulation)
    {
        var fichier = RegleDesMedias.Verifier(requete.Cle, requete.Genre, requete.Contenu.Span);

        // Le fichier est rangé d'abord : si la base échoue ensuite, il reste un fichier inutilisé, jamais un lien cassé.
        await stockage.DeposerAsync(fichier, requete.Contenu, annulation);

        var existants = (await medias.ListerAsync(annulation)).ToList();
        var existant = existants.Find(m => m.Cle == requete.Cle);
        if (existant is null)
        {
            existant = MediaDuSite.Creer(requete.Cle, fichier, horloge.Instant);
            medias.Ajouter(existant);
            existants.Add(existant);
        }
        else
        {
            existant.Remplacer(fichier, horloge.Instant);
        }

        MediasPublies.PublierApresEnregistrement(existants, copies, apresEnregistrement);
        return fichier.Nom;
    }
}

/// <summary>Rend à un emplacement son fichier d'origine. Renvoie faux s'il n'avait pas été remplacé.</summary>
public sealed record RetirerMedia(string Cle) : ICommande<bool>;

public sealed class RetirerMediaGestionnaire(
    IMediasDuSiteRepository medias,
    ICopiesDeLecture copies,
    ActionsApresEnregistrement apresEnregistrement) : IGestionnaire<RetirerMedia, bool>
{
    public async Task<bool> TraiterAsync(RetirerMedia requete, CancellationToken annulation)
    {
        var existants = (await medias.ListerAsync(annulation)).ToList();
        var existant = existants.Find(m => m.Cle == requete.Cle);
        if (existant is null)
            return false;

        // Le fichier reste dans le stockage (et dans son historique) : seul le lien disparaît.
        medias.Supprimer(existant);
        existants.Remove(existant);
        MediasPublies.PublierApresEnregistrement(existants, copies, apresEnregistrement);
        return true;
    }
}

/// <summary>Les médias envoyés, lus dans la base : pour l'administration.</summary>
public sealed record ObtenirMediasDuSite : IConsultation<MediasPublies>;

public sealed class ObtenirMediasDuSiteGestionnaire(IMediasDuSiteRepository medias)
    : IGestionnaire<ObtenirMediasDuSite, MediasPublies>
{
    public async Task<MediasPublies> TraiterAsync(ObtenirMediasDuSite requete, CancellationToken annulation) =>
        MediasPublies.Depuis(await medias.ListerAsync(annulation));
}

/// <summary>
/// Les médias envoyés, lus dans leur copie : pour les pages publiques, sans réveiller la base.
/// Si la copie n'existe pas encore, elle est reconstruite une fois depuis la base (même vide).
/// </summary>
public sealed record ObtenirMediasPublies : IConsultation<MediasPublies>;

public sealed class ObtenirMediasPubliesGestionnaire(ICopiesDeLecture copies, IMediasDuSiteRepository medias)
    : IGestionnaire<ObtenirMediasPublies, MediasPublies>
{
    public async Task<MediasPublies> TraiterAsync(ObtenirMediasPublies requete, CancellationToken annulation)
    {
        var publies = await copies.LireAsync<MediasPublies>(NomsDesCopies.MediasDuSite, annulation);
        if (publies is not null)
            return publies;

        var reconstruits = MediasPublies.Depuis(await medias.ListerAsync(annulation));
        await copies.PublierAsync(NomsDesCopies.MediasDuSite, reconstruits, annulation);
        return reconstruits;
    }
}

/// <summary>Un média envoyé, tel que le lisent les pages : son fichier, son type et sa date.</summary>
public sealed record MediaPublie(string Fichier, string TypeContenu, DateTimeOffset ModifieLe);

/// <summary>Les médias envoyés, emplacement par emplacement : ce que publie la copie de lecture.</summary>
public sealed record MediasPublies(IReadOnlyDictionary<string, MediaPublie> Medias)
{
    public static readonly MediasPublies Aucun = new(new Dictionary<string, MediaPublie>(StringComparer.Ordinal));

    public static MediasPublies Depuis(IEnumerable<MediaDuSite> medias) =>
        new(medias.ToDictionary(m => m.Cle, m => new MediaPublie(m.Fichier, m.TypeContenu, m.ModifieLe), StringComparer.Ordinal));

    /// <summary>La copie n'est réécrite qu'une fois la base enregistrée.</summary>
    internal static void PublierApresEnregistrement(
        IEnumerable<MediaDuSite> medias,
        ICopiesDeLecture copies,
        ActionsApresEnregistrement apresEnregistrement)
    {
        var copie = Depuis(medias);
        apresEnregistrement.Ajouter(annulation => copies.PublierAsync(NomsDesCopies.MediasDuSite, copie, annulation));
    }
}
