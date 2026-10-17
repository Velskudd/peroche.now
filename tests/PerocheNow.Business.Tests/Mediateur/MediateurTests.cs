using Microsoft.Extensions.DependencyInjection;
using PerocheNow.Business.Mediateur;

namespace PerocheNow.Business.Tests.Mediateur;

public sealed class MediateurTests
{
    public sealed record Saluer(string Nom) : IConsultation<string>;

    public sealed record RequeteOrpheline : IConsultation<int>;

    public sealed class SaluerGestionnaire(Journal journal) : IGestionnaire<Saluer, string>
    {
        public Task<string> TraiterAsync(Saluer requete, CancellationToken annulation)
        {
            journal.Etapes.Add("gestionnaire");
            return Task.FromResult($"Salut {requete.Nom}");
        }
    }

    public sealed class Journal
    {
        public List<string> Etapes { get; } = [];
    }

    public sealed class Exterieur<TRequete, TReponse>(Journal journal) : IComportement<TRequete, TReponse>
        where TRequete : IRequete<TReponse>
    {
        public async Task<TReponse> TraiterAsync(TRequete requete, SuiteDuTraitement<TReponse> suite, CancellationToken annulation)
        {
            journal.Etapes.Add("extérieur avant");
            var reponse = await suite();
            journal.Etapes.Add("extérieur après");
            return reponse;
        }
    }

    public sealed class Interieur<TRequete, TReponse>(Journal journal) : IComportement<TRequete, TReponse>
        where TRequete : IRequete<TReponse>
    {
        public async Task<TReponse> TraiterAsync(TRequete requete, SuiteDuTraitement<TReponse> suite, CancellationToken annulation)
        {
            journal.Etapes.Add("intérieur avant");
            var reponse = await suite();
            journal.Etapes.Add("intérieur après");
            return reponse;
        }
    }

    private static ServiceProvider Construire(bool avecComportements)
    {
        var services = new ServiceCollection();
        services.AddScoped<IMediateur, Business.Mediateur.Mediateur>();
        services.AddScoped<Journal>();
        services.AjouterGestionnaires(typeof(MediateurTests).Assembly);
        if (avecComportements)
        {
            services.AddScoped(typeof(IComportement<,>), typeof(Exterieur<,>));
            services.AddScoped(typeof(IComportement<,>), typeof(Interieur<,>));
        }
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Envoie_la_requete_a_son_gestionnaire()
    {
        using var fournisseur = Construire(avecComportements: false);
        using var portee = fournisseur.CreateScope();

        var reponse = await portee.ServiceProvider.GetRequiredService<IMediateur>().EnvoyerAsync(new Saluer("Alexandre"));

        Assert.Equal("Salut Alexandre", reponse);
    }

    [Fact]
    public async Task Le_premier_comportement_enregistre_est_le_plus_exterieur()
    {
        using var fournisseur = Construire(avecComportements: true);
        using var portee = fournisseur.CreateScope();

        await portee.ServiceProvider.GetRequiredService<IMediateur>().EnvoyerAsync(new Saluer("Alexandre"));

        string[] attendu = ["extérieur avant", "intérieur avant", "gestionnaire", "intérieur après", "extérieur après"];
        Assert.Equal(attendu, portee.ServiceProvider.GetRequiredService<Journal>().Etapes);
    }

    [Fact]
    public async Task Une_requete_sans_gestionnaire_est_signalee_clairement()
    {
        using var fournisseur = Construire(avecComportements: false);
        using var portee = fournisseur.CreateScope();

        var erreur = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            portee.ServiceProvider.GetRequiredService<IMediateur>().EnvoyerAsync(new RequeteOrpheline()));

        Assert.Contains(nameof(RequeteOrpheline), erreur.Message);
    }
}
