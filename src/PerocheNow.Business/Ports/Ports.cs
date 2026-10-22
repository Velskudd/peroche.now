using PerocheNow.Business.Contenus;
using PerocheNow.Domain.Audit;
using PerocheNow.Domain.Maintenant;
using PerocheNow.Domain.Plateformes;

namespace PerocheNow.Business.Ports;

// Les ports décrivent ce dont la couche Business a besoin.
// Leurs implémentations vivent dans l'Infrastructure, qui dépend de Business et jamais l'inverse.

/// <summary>Accès au statut du moment. Il n'en existe qu'un seul à la fois.</summary>
public interface IStatutDuMomentRepository
{
    Task<StatutDuMoment?> ObtenirAsync(CancellationToken annulation);
    void Ajouter(StatutDuMoment statut);
}

/// <summary>Stockage des traces d'audit.</summary>
public interface ITraceAuditRepository
{
    void Ajouter(TraceAudit trace);
}

/// <summary>Valide en une fois toutes les modifications en attente.</summary>
public interface IUniteDeTravail
{
    Task EnregistrerAsync(CancellationToken annulation);
}

/// <summary>L'heure courante, isolée pour pouvoir la maîtriser dans les tests.</summary>
public interface IHorloge
{
    DateTimeOffset Instant { get; }
}

/// <summary>Qui agit. Renverra l'utilisateur connecté quand le service de rôles sera en place.</summary>
public interface IUtilisateurCourant
{
    string Nom { get; }
}

/// <summary>La plateforme de diffusion (Twitch) : le direct en cours, ou <c>null</c> hors direct.</summary>
public interface ISourceDuDirect
{
    Task<DirectEnCours?> ObtenirAsync(CancellationToken annulation);
}

/// <summary>Un direct en cours, tel que la plateforme le décrit.</summary>
public sealed record DirectEnCours(string Titre, string? Jeu, DateTimeOffset DebutLe);

/// <summary>
/// Une plateforme de contenus (YouTube, Instagram…). Chaque implémentation adapte l'API ou le flux
/// de sa plateforme en <see cref="Contenu"/>.
/// </summary>
public interface ISourceDeContenus
{
    Task<IReadOnlyList<Contenu>> ObtenirAsync(CancellationToken annulation);
}

/// <summary>L'accès au compte Instagram (jeton). Il n'en existe qu'un à la fois.</summary>
public interface IAccesInstagramRepository
{
    Task<AccesInstagram?> ObtenirAsync(CancellationToken annulation);
    void Ajouter(AccesInstagram acces);
}

/// <summary>L'API Instagram, vue par le site : vérifier un jeton, le prolonger, lire les publications.</summary>
public interface IApiInstagram
{
    /// <summary>Le nom du compte auquel le jeton donne accès, ou <c>null</c> si Instagram refuse le jeton.</summary>
    Task<string?> IdentifierCompteAsync(string jeton, CancellationToken annulation);

    /// <summary>Un nouveau jeton longue durée, à partir d'un jeton encore valide.</summary>
    Task<JetonInstagram> RenouvelerAsync(string jeton, CancellationToken annulation);

    Task<IReadOnlyList<Contenu>> ObtenirPublicationsAsync(string jeton, CancellationToken annulation);
}

/// <summary>Un jeton délivré par Instagram et sa durée de validité.</summary>
public sealed record JetonInstagram(string Valeur, TimeSpan Validite)
{
    /// <summary>Jamais le jeton dans un journal.</summary>
    public override string ToString() => $"Jeton Instagram ({Validite.TotalDays:0} jours)";
}
