using Microsoft.Extensions.DependencyInjection;
using PerocheNow.Business.Ports;
using IMediateur = PerocheNow.Business.Mediateur.IMediateur;
using PerocheNow.Domain.Audit;
using PerocheNow.Domain.Maintenant;

namespace PerocheNow.Business.Tests;

// Doublures des ports : elles remplacent l'Infrastructure pour tester la couche Business seule.

internal sealed class FauxStatuts : IStatutDuMomentRepository
{
    public StatutDuMoment? Statut { get; set; }

    public Task<StatutDuMoment?> ObtenirAsync(CancellationToken annulation) => Task.FromResult(Statut);

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

/// <summary>La couche Business complète, branchée sur les doublures.</summary>
internal sealed class Banc : IDisposable
{
    public static readonly DateTimeOffset Instant = new(2026, 10, 17, 11, 32, 0, TimeSpan.Zero);

    private readonly ServiceProvider _fournisseur;

    public FauxStatuts Statuts { get; } = new();
    public FaussesTraces Traces { get; } = new();
    public FausseUniteDeTravail UniteDeTravail { get; } = new();
    public HorlogeFixe Horloge { get; } = new(Instant);

    public Banc(Action<IServiceCollection>? configurer = null)
    {
        var services = new ServiceCollection();
        services.AjouterBusiness();
        services.AddSingleton<IStatutDuMomentRepository>(Statuts);
        services.AddSingleton<ITraceAuditRepository>(Traces);
        services.AddSingleton<IUniteDeTravail>(UniteDeTravail);
        services.AddSingleton<IHorloge>(Horloge);
        services.AddSingleton<IUtilisateurCourant>(new UtilisateurFixe("alexandre"));
        configurer?.Invoke(services);

        _fournisseur = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    public IMediateur Mediateur(IServiceScope portee) => portee.ServiceProvider.GetRequiredService<IMediateur>();

    public IServiceScope NouvellePortee() => _fournisseur.CreateScope();

    public void Dispose() => _fournisseur.Dispose();
}
