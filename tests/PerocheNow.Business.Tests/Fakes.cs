using Microsoft.Extensions.DependencyInjection;
using PerocheNow.Business.Contenus;
using PerocheNow.Business.Ports;
using IMediateur = PerocheNow.Business.Mediateur.IMediateur;
using PerocheNow.Domain.Audit;
using PerocheNow.Domain.Maintenant;
using PerocheNow.Domain.Medias;
using PerocheNow.Domain.Plateformes;
using PerocheNow.Domain.Textes;

namespace PerocheNow.Business.Tests;

// Doublures des ports : elles remplacent l'Infrastructure pour tester la couche Business seule.

internal sealed class FauxStatuts : IStatutDuMomentRepository
{
    public StatutDuMoment? Statut { get; set; }

    /// <summary>Combien de fois la base a été lue : chaque lecture réveillerait la vraie base.</summary>
    public int Lectures { get; private set; }

    public Task<StatutDuMoment?> ObtenirAsync(CancellationToken annulation)
    {
        Lectures++;
        return Task.FromResult(Statut);
    }

    public void Ajouter(StatutDuMoment statut) => Statut = statut;
}

internal sealed class FaussesTraces : ITraceAuditRepository
{
    public List<TraceAudit> Traces { get; } = [];

    public void Ajouter(TraceAudit trace) => Traces.Add(trace);
}

internal sealed class FausseUniteDeTravail : IUniteDeTravail
{
    public int Enregistrements { get; private set; }

    public Task EnregistrerAsync(CancellationToken annulation)
    {
        Enregistrements++;
        return Task.CompletedTask;
    }
}

internal sealed class HorlogeFixe(DateTimeOffset instant) : IHorloge
{
    public DateTimeOffset Instant { get; set; } = instant;
}

internal sealed class UtilisateurFixe(string nom) : IUtilisateurCourant
{
    public string Nom { get; } = nom;
}

internal sealed class FauxAccesInstagram : IAccesInstagramRepository
{
    public AccesInstagram? Acces { get; set; }

    public int Lectures { get; private set; }

    public Task<AccesInstagram?> ObtenirAsync(CancellationToken annulation)
    {
        Lectures++;
        return Task.FromResult(Acces);
    }

    public void Ajouter(AccesInstagram acces) => Acces = acces;
}

/// <summary>Instagram simulé : un seul jeton valide, et un compteur de renouvellements.</summary>
internal sealed class FausseApiInstagram : IApiInstagram
{
    public const string JetonValide = "IGAAvalide";
    public int Renouvellements { get; private set; }

    public Task<string?> IdentifierCompteAsync(string jeton, CancellationToken annulation) =>
        Task.FromResult(jeton == JetonValide ? "peroche" : null);

    public Task<JetonInstagram> RenouvelerAsync(string jeton, CancellationToken annulation)
    {
        Renouvellements++;
        return Task.FromResult(new JetonInstagram($"IGAArenouvele{Renouvellements}", TimeSpan.FromDays(60)));
    }

    public Task<IReadOnlyList<Contenu>> ObtenirPublicationsAsync(string jeton, CancellationToken annulation) =>
        Task.FromResult<IReadOnlyList<Contenu>>([]);
}

internal sealed class FauxMedias : IMediasDuSiteRepository
{
    public List<MediaDuSite> Medias { get; } = [];

    public int Lectures { get; private set; }

    public Task<IReadOnlyList<MediaDuSite>> ListerAsync(CancellationToken annulation)
    {
        Lectures++;
        return Task.FromResult<IReadOnlyList<MediaDuSite>>(Medias.ToList());
    }

    public void Ajouter(MediaDuSite media) => Medias.Add(media);

    public void Supprimer(MediaDuSite media) => Medias.Remove(media);
}

/// <summary>Un stockage en mémoire qui note chaque dépôt et ce que la base avait déjà enregistré à ce moment-là.</summary>
internal sealed class FauxStockageDeMedias(FausseUniteDeTravail uniteDeTravail) : IStockageDeMedias
{
    public Dictionary<string, byte[]> Fichiers { get; } = [];

    public List<(string Nom, int EnregistrementsAvant)> Depots { get; } = [];

    public Task DeposerAsync(FichierMedia fichier, ReadOnlyMemory<byte> contenu, CancellationToken annulation)
    {
        Fichiers[fichier.Nom] = contenu.ToArray();
        Depots.Add((fichier.Nom, uniteDeTravail.Enregistrements));
        return Task.CompletedTask;
    }

    public Task<MediaOuvert?> OuvrirAsync(string nom, string? plage, CancellationToken annulation) =>
        Task.FromResult<MediaOuvert?>(null);
}

internal sealed class FauxTextes : ITextesDuSiteRepository
{
    public List<TexteDuSite> Textes { get; } = [];

    public int Lectures { get; private set; }

    public Task<IReadOnlyList<TexteDuSite>> ListerAsync(CancellationToken annulation)
    {
        Lectures++;
        return Task.FromResult<IReadOnlyList<TexteDuSite>>(Textes.ToList());
    }

    public void Ajouter(TexteDuSite texte) => Textes.Add(texte);

    public void Supprimer(TexteDuSite texte) => Textes.Remove(texte);
}

/// <summary>
/// Copies de lecture en mémoire. Chaque publication note combien d'enregistrements la base avait déjà reçus,
/// pour vérifier qu'une copie n'est jamais publiée avant la base.
/// </summary>
internal sealed class FaussesCopies(FausseUniteDeTravail uniteDeTravail) : ICopiesDeLecture
{
    private readonly Dictionary<string, object> _copies = [];

    public List<(string Nom, int EnregistrementsAvant)> Publications { get; } = [];

    public bool EnPanne { get; set; }

    public T? Copie<T>(string nom) where T : class => _copies.TryGetValue(nom, out var copie) ? (T)copie : null;

    public void Deposer<T>(string nom, T copie) where T : class => _copies[nom] = copie;

    public Task<T?> LireAsync<T>(string nom, CancellationToken annulation) where T : class =>
        Task.FromResult(Copie<T>(nom));

    public Task PublierAsync<T>(string nom, T copie, CancellationToken annulation) where T : class
    {
        if (EnPanne)
            throw new HttpRequestException("stockage injoignable");

        _copies[nom] = copie;
        Publications.Add((nom, uniteDeTravail.Enregistrements));
        return Task.CompletedTask;
    }
}

internal sealed class FauxDirect : ISourceDuDirect
{
    public DirectEnCours? Direct { get; set; }

    public Task<DirectEnCours?> ObtenirAsync(CancellationToken annulation) => Task.FromResult(Direct);
}

/// <summary>La couche Business complète, branchée sur les doublures.</summary>
internal sealed class Banc : IDisposable
{
    public static readonly DateTimeOffset Instant = new(2026, 10, 17, 11, 32, 0, TimeSpan.Zero);

    private readonly ServiceProvider _fournisseur;

    public FauxStatuts Statuts { get; } = new();
    public FaussesTraces Traces { get; } = new();
    public FausseUniteDeTravail UniteDeTravail { get; } = new();
    public HorlogeFixe Horloge { get; } = new(Instant);
    public FauxDirect Direct { get; } = new();
    public FauxAccesInstagram AccesInstagram { get; } = new();
    public FausseApiInstagram Instagram { get; } = new();
    public FaussesCopies Copies { get; }
    public FauxTextes Textes { get; } = new();
    public FauxMedias Medias { get; } = new();
    public FauxStockageDeMedias Stockage { get; }

    public Banc(Action<IServiceCollection>? configurer = null)
    {
        Copies = new FaussesCopies(UniteDeTravail);
        Stockage = new FauxStockageDeMedias(UniteDeTravail);

        var services = new ServiceCollection();
        services.AjouterBusiness();
        services.AddSingleton<IStatutDuMomentRepository>(Statuts);
        services.AddSingleton<ITraceAuditRepository>(Traces);
        services.AddSingleton<IUniteDeTravail>(UniteDeTravail);
        services.AddSingleton<IHorloge>(Horloge);
        services.AddSingleton<IUtilisateurCourant>(new UtilisateurFixe("alexandre"));
        services.AddSingleton<ISourceDuDirect>(Direct);
        services.AddSingleton<IAccesInstagramRepository>(AccesInstagram);
        services.AddSingleton<IApiInstagram>(Instagram);
        services.AddSingleton<ICopiesDeLecture>(Copies);
        services.AddSingleton<ITextesDuSiteRepository>(Textes);
        services.AddSingleton<IMediasDuSiteRepository>(Medias);
        services.AddSingleton<IStockageDeMedias>(Stockage);
        configurer?.Invoke(services);

        _fournisseur = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    public static IMediateur Mediateur(IServiceScope portee) => portee.ServiceProvider.GetRequiredService<IMediateur>();

    public IServiceScope NouvellePortee() => _fournisseur.CreateScope();

    public void Dispose() => _fournisseur.Dispose();
}
