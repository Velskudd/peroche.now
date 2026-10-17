using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PerocheNow.Business;
using PerocheNow.Business.Maintenant;
using PerocheNow.Business.Mediateur;
using PerocheNow.Domain.Maintenant;
using PerocheNow.Infrastructure.Persistance;

namespace PerocheNow.Infrastructure.Tests;

/// <summary>
/// Vérifie le vrai câblage EF Core de bout en bout, sur une base SQLite en mémoire :
/// configuration, initialisation, commandes passées par le médiateur et relecture dans une nouvelle portée.
/// </summary>
public sealed class PersistanceTests : IAsyncLifetime
{
    private readonly string _chaine = $"Data Source=perochenow-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    // Une base SQLite en mémoire disparaît avec sa dernière connexion : celle-ci la garde en vie le temps du test.
    private SqliteConnection _gardien = null!;
    private ServiceProvider _services = null!;

    public async Task InitializeAsync()
    {
        _gardien = new SqliteConnection(_chaine);
        await _gardien.OpenAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PerocheNow"] = _chaine,
                ["BaseDeDonnees:Fournisseur"] = "Sqlite",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AjouterBusiness();
        services.AjouterInfrastructure(configuration);
        _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await _services.InitialiserBaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        await _gardien.DisposeAsync();
    }

    private async Task<T> DansUnePorteeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        await using var portee = _services.CreateAsyncScope();
        return await action(portee.ServiceProvider);
    }

    private Task<StatutDuMomentDto?> LireStatutAsync() =>
        DansUnePorteeAsync(sp => sp.GetRequiredService<IMediateur>().EnvoyerAsync(new ObtenirStatutDuMoment()));

    private const string CompterStatuts = "SELECT COUNT(*) AS \"Value\" FROM \"StatutDuMoment\"";
    private const string CompterLignes = "SELECT COUNT(*) AS \"Value\" FROM \"LigneDuMoment\"";
    private const string CompterTraces = "SELECT COUNT(*) AS \"Value\" FROM \"TraceAudit\"";

    private Task<int> CompterAsync(string requeteSql) =>
        DansUnePorteeAsync(sp => sp.GetRequiredService<PerocheNowDbContext>().Database
            .SqlQueryRaw<int>(requeteSql)
            .SingleAsync());

    [Fact]
    public async Task Une_base_vide_recoit_le_statut_initial_et_sa_trace()
    {
        var statut = await LireStatutAsync();

        Assert.NotNull(statut);
        Assert.Equal(StatutInitial.Commande.Accroche, statut.Accroche);
        Assert.Equal(
            StatutInitial.Commande.Lignes.Select(l => new LigneDuMomentDto(l.Texte, l.Rubrique)),
            statut.Lignes);
        Assert.Equal(StatutInitial.Commande.Objectif, statut.ProchainObjectif?.Libelle);
        Assert.Null(statut.ProchainObjectif?.DateCible);

        var trace = await DansUnePorteeAsync(sp => sp.GetRequiredService<PerocheNowDbContext>().TracesAudit.SingleAsync());
        Assert.Equal(nameof(DefinirStatutDuMoment), trace.Action);
        Assert.Equal("système", trace.Auteur);
    }

    [Fact]
    public async Task Relancer_l_initialisation_ne_duplique_rien()
    {
        await _services.InitialiserBaseAsync();

        Assert.Equal(1, await CompterAsync(CompterStatuts));
        Assert.Equal(1, await CompterAsync(CompterTraces));
        Assert.Equal(StatutInitial.Commande.Lignes.Count, await CompterAsync(CompterLignes));
    }

    [Fact]
    public async Task Definir_remplace_les_lignes_et_l_objectif_en_base()
    {
        await DansUnePorteeAsync(sp => sp.GetRequiredService<IMediateur>().EnvoyerAsync(
            new DefinirStatutDuMoment(
                "Je reviens de la Test Week.",
                [new LigneSaisie("Débrief des données", Rubrique.ProjetEnCours), new LigneSaisie("Montage du vlog", Rubrique.Contenus)],
                "RCN 2027",
                new DateOnly(2027, 4, 10))));

        var statut = await LireStatutAsync();

        Assert.NotNull(statut);
        Assert.Equal("Je reviens de la Test Week.", statut.Accroche);
        LigneDuMomentDto[] lignesAttendues =
        [
            new("Débrief des données", Rubrique.ProjetEnCours),
            new("Montage du vlog", Rubrique.Contenus),
        ];
        Assert.Equal(lignesAttendues, statut.Lignes);
        Assert.Equal(new ObjectifDto("RCN 2027", new DateOnly(2027, 4, 10)), statut.ProchainObjectif);

        // Les anciennes lignes sont supprimées, pas seulement détachées.
        Assert.Equal(2, await CompterAsync(CompterLignes));
        Assert.Equal(2, await CompterAsync(CompterTraces));
    }

    [Fact]
    public async Task Retirer_l_objectif_est_bien_persiste()
    {
        await DansUnePorteeAsync(sp => sp.GetRequiredService<IMediateur>().EnvoyerAsync(
            new DefinirStatutDuMoment("Pause entre deux projets.", [], Objectif: null, DateObjectif: null)));

        var statut = await LireStatutAsync();

        Assert.NotNull(statut);
        Assert.Null(statut.ProchainObjectif);
        Assert.Empty(statut.Lignes);
    }

    [Fact]
    public void Le_modele_se_traduit_en_schema_SQL_Server()
    {
        var options = new DbContextOptionsBuilder<PerocheNowDbContext>()
            .UseSqlServer("Server=localhost;Database=PerocheNow;Trusted_Connection=True;")
            .Options;
        using var db = new PerocheNowDbContext(options);

        var script = db.Database.GenerateCreateScript();

        Assert.Contains("CREATE TABLE [StatutDuMoment]", script);
        Assert.Contains("CREATE TABLE [LigneDuMoment]", script);
        Assert.Contains("CREATE TABLE [TraceAudit]", script);
        Assert.Contains("[ObjectifLibelle] nvarchar(80)", script);
        Assert.Contains("[Accroche] nvarchar(140) NOT NULL", script);
        Assert.Contains("[Rubrique] nvarchar(30) NOT NULL", script);
    }
}
