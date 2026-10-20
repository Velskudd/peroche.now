using PerocheNow.Business.Mediateur;
using PerocheNow.Business.Ports;

namespace PerocheNow.Business.Direct;

/// <summary>Le direct Twitch en cours, s'il y en a un : il allume la pastille « En direct ».</summary>
public sealed record ObtenirDirect : IConsultation<DirectEnCours?>;

public sealed class ObtenirDirectGestionnaire(ISourceDuDirect source) : IGestionnaire<ObtenirDirect, DirectEnCours?>
{
    public Task<DirectEnCours?> TraiterAsync(ObtenirDirect requete, CancellationToken annulation) =>
        source.ObtenirAsync(annulation);
}
