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
    IHorloge horloge) : IGestionnaire<ConnecterInstagram, Guid>
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
    IHorloge horloge) : IGestionnaire<RenouvelerAccesInstagram, Guid?>
{
    public async Task<Guid?> TraiterAsync(RenouvelerAccesInstagram requete, CancellationToken annulation)
    {
        var existant = await acces.ObtenirAsync(annulation);
        if (existant is null || !existant.DoitEtreRenouvele(horloge.Instant))
            return null;

        var nouveau = await instagram.RenouvelerAsync(existant.Jeton, annulation);
        var maintenant = horloge.Instant;
        existant.Renouveler(nouveau.Valeur, maintenant + nouveau.Validite, maintenant);
        return existant.Id;
    }
}

/// <summary>Où en est l'accès Instagram, pour l'administration et le renouvellement automatique.</summary>
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
