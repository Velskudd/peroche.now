using Microsoft.Extensions.DependencyInjection;
using PerocheNow.Business.Contenus;
using PerocheNow.Business.Ports;

namespace PerocheNow.Business.Tests.Contenus;

public sealed class ObtenirDerniersContenusTests
{
    private static Contenu Exemple(Plateforme plateforme, string titre, int jour) =>
        new(plateforme, FormatDeContenu.Court, titre, new UriBuilder(Uri.UriSchemeHttps, "contenu.test", -1, titre).Uri, null,
            new DateTimeOffset(2026, 10, jour, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Sans_source_la_liste_est_vide()
    {
        using var banc = new Banc();
        using var portee = banc.NouvellePortee();

        var contenus = await Banc.Mediateur(portee).EnvoyerAsync(new ObtenirDerniersContenus());

        Assert.Empty(contenus);
    }

    [Fact]
    public async Task Les_contenus_de_toutes_les_sources_sont_fusionnes_du_plus_recent_au_plus_ancien()
    {
        var youtube = new SourceFixe(Exemple(Plateforme.YouTube, "a", 18), Exemple(Plateforme.YouTube, "c", 22));
        var instagram = new SourceFixe(Exemple(Plateforme.Instagram, "b", 20));
        using var banc = new Banc(services =>
        {
            services.AddSingleton<ISourceDeContenus>(youtube);
            services.AddSingleton<ISourceDeContenus>(instagram);
        });
        using var portee = banc.NouvellePortee();

        var contenus = await Banc.Mediateur(portee).EnvoyerAsync(new ObtenirDerniersContenus());

        Assert.Equal(new[] { "c", "b", "a" }, contenus.Select(c => c.Titre).ToArray());
        Assert.Empty(banc.Traces.Traces);
    }

    private sealed class SourceFixe(params Contenu[] contenus) : ISourceDeContenus
    {
        public Task<IReadOnlyList<Contenu>> ObtenirAsync(CancellationToken annulation) =>
            Task.FromResult<IReadOnlyList<Contenu>>(contenus);
    }
}
