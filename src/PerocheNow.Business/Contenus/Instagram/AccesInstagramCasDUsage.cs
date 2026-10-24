using PerocheNow.Business.Audit;
using PerocheNow.Business.CopiesDeLecture;
using PerocheNow.Business.Mediateur;
using PerocheNow.Business.Ports;
using PerocheNow.Domain.Commun;
using PerocheNow.Domain.Plateformes;

namespace PerocheNow.Business.Contenus.Instagram;

/// <summary>
/// Branche le compte Instagram avec un jeton longue durée, saisi dans l'administration.
/// Le jeton est d'abord vérifié auprès d'Instagram. Renvoie l'identifiant de l'accès.
/// </summary>
public sealed record ConnecterInstagram(string Jeton) : ICommande<Guid>
{
    /// <summary>Jamais le jeton dans un journal.</summary>
    public override string ToString() => nameof(ConnecterInstagram);
}

public sealed class ConnecterInstagramGestionnaire(
    IAccesInstagramRepository acces,
    IApiInstagram instagram,
    IHorloge horloge,
    ICopiesDeLecture copies,
    ActionsApresEnregistrement apresEnregistrement) : IGestionnaire<ConnecterInstagram, Guid>
{
    public async Task<Guid> TraiterAsync(ConnecterInstagram requete, CancellationToken annulation)
    {
        if (string.IsNullOrWhiteSpace(requete.Jeton))
            throw new RegleMetierException("Colle le jeton d'accès Instagram.");

        var jeton = requete.Jeton.Trim();

        var compte = await instagram.IdentifierCompteAsync(jeton, annulation)
            ?? throw new RegleMetierException("Instagram refuse ce jeton : vérifie qu'il est complet et qu'il n'a pas expiré.");

        var maintenant = horloge.Instant;
        var expireLe = maintenant + AccesInstagram.DureeDuJeton;
        var existant = await acces.ObtenirAsync(annulation);

        if (existant is null)
        {
            existant = AccesInstagram.Creer(jeton, compte, expireLe, maintenant);
            acces.Ajouter(existant);
        }
        else
        {
            existant.Remplacer(jeton, compte, expireLe, maintenant);
        }

        CopieAccesInstagram.PublierApresEnregistrement(existant, copies, apresEnregistrement);
        return existant.Id;
    }
}

/// <summary>
/// Prolonge le jeton Instagram de 60 jours, s'il est assez ancien pour qu'Instagram l'accepte.
/// Renvoie l'identifiant de l'accès renouvelé, ou <c>null</c> s'il n'y avait rien à faire.
/// </summary>
public sealed record RenouvelerAccesInstagram : ICommande<Guid?>;

public sealed class RenouvelerAccesInstagramGestionnaire(
    IAccesInstagramRepository acces,
    IApiInstagram instagram,
    IHorloge horloge,
    ICopiesDeLecture copies,
    ActionsApresEnregistrement apresEnregistrement) : IGestionnaire<RenouvelerAccesInstagram, Guid?>
{
    public async Task<Guid?> TraiterAsync(RenouvelerAccesInstagram requete, CancellationToken annulation)
    {
        var existant = await acces.ObtenirAsync(annulation);
        if (existant is null || !existant.DoitEtreRenouvele(horloge.Instant))
            return null;

        var nouveau = await instagram.RenouvelerAsync(existant.Jeton, annulation);
        var maintenant = horloge.Instant;
        existant.Renouveler(nouveau.Valeur, maintenant + nouveau.Validite, maintenant);

        CopieAccesInstagram.PublierApresEnregistrement(existant, copies, apresEnregistrement);
        return existant.Id;
    }
}

/// <summary>Où en est l'accès Instagram, lu dans la base : pour l'administration.</summary>
public sealed record ObtenirEtatInstagram : IConsultation<EtatInstagram?>;

public sealed record EtatInstagram(
    string Compte,
    DateTimeOffset ObtenuLe,
    DateTimeOffset ExpireLe,
    bool EstExpire,
    bool DoitEtreRenouvele);

public sealed class ObtenirEtatInstagramGestionnaire(IAccesInstagramRepository acces, IHorloge horloge)
    : IGestionnaire<ObtenirEtatInstagram, EtatInstagram?>
{
    public async Task<EtatInstagram?> TraiterAsync(ObtenirEtatInstagram requete, CancellationToken annulation)
    {
        var existant = await acces.ObtenirAsync(annulation);
        if (existant is null)
            return null;

        var maintenant = horloge.Instant;
        return new EtatInstagram(
            existant.Compte,
            existant.ObtenuLe,
            existant.ExpireLe,
            existant.EstExpire(maintenant),
            existant.DoitEtreRenouvele(maintenant));
    }
}

/// <summary>
/// L'accès Instagram lu dans sa copie : de quoi lire les publications et savoir s'il faut renouveler le jeton,
/// sans réveiller la base. Si la copie n'existe pas encore, elle est reconstruite une fois depuis la base,
/// y compris quand aucun compte n'est branché (pour ne pas reposer la question à chaque lecture).
/// </summary>
public sealed record ObtenirAccesInstagramPublie : IConsultation<CopieAccesInstagram?>;

public sealed class ObtenirAccesInstagramPublieGestionnaire(ICopiesDeLecture copies, IAccesInstagramRepository acces)
    : IGestionnaire<ObtenirAccesInstagramPublie, CopieAccesInstagram?>
{
    public async Task<CopieAccesInstagram?> TraiterAsync(ObtenirAccesInstagramPublie requete, CancellationToken annulation)
    {
        var publie = await copies.LireAsync<AccesInstagramPublie>(NomsDesCopies.AccesInstagram, annulation);
        if (publie is not null)
            return publie.Acces;

        var existant = await acces.ObtenirAsync(annulation);
        var copie = existant is null ? null : CopieAccesInstagram.Depuis(existant);
        await copies.PublierAsync(NomsDesCopies.AccesInstagram, new AccesInstagramPublie(copie), annulation);
        return copie;
    }
}

/// <summary>Ce que contient la copie de l'accès Instagram. <see cref="Acces"/> vaut <c>null</c> sans compte branché.</summary>
public sealed record AccesInstagramPublie(CopieAccesInstagram? Acces);

/// <summary>L'accès Instagram tel que copié hors de la base.</summary>
public sealed record CopieAccesInstagram(string Jeton, string Compte, DateTimeOffset ObtenuLe, DateTimeOffset ExpireLe)
{
    public bool EstExpire(DateTimeOffset maintenant) => AccesInstagram.Expire(ExpireLe, maintenant);

    public bool DoitEtreRenouvele(DateTimeOffset maintenant) => AccesInstagram.ARenouveler(ObtenuLe, ExpireLe, maintenant);

    public static CopieAccesInstagram Depuis(AccesInstagram acces) =>
        new(acces.Jeton, acces.Compte, acces.ObtenuLe, acces.ExpireLe);

    /// <summary>Réécrit la copie une fois l'accès enregistré en base.</summary>
    internal static void PublierApresEnregistrement(
        AccesInstagram acces,
        ICopiesDeLecture copies,
        ActionsApresEnregistrement apresEnregistrement)
    {
        var copie = new AccesInstagramPublie(Depuis(acces));
        apresEnregistrement.Ajouter(annulation => copies.PublierAsync(NomsDesCopies.AccesInstagram, copie, annulation));
    }

    /// <summary>Jamais le jeton dans un journal.</summary>
    public override string ToString() => $"Accès Instagram de {Compte}, valable jusqu'au {ExpireLe:yyyy-MM-dd}";
}
