using PerocheNow.Business.Direct;
using PerocheNow.Business.Ports;

namespace PerocheNow.Business.Tests.Direct;

public sealed class ObtenirDirectTests
{
    [Fact]
    public async Task Hors_direct_la_consultation_ne_renvoie_rien()
    {
        using var banc = new Banc();
        using var portee = banc.NouvellePortee();

        var direct = await Banc.Mediateur(portee).EnvoyerAsync(new ObtenirDirect());

        Assert.Null(direct);
    }

    [Fact]
    public async Task Pendant_un_direct_la_consultation_le_renvoie_sans_trace_d_audit()
    {
        using var banc = new Banc();
        banc.Direct.Direct = new DirectEnCours("NEC, manche 9", "iRacing", Banc.Instant.AddMinutes(-20));
        using var portee = banc.NouvellePortee();

        var direct = await Banc.Mediateur(portee).EnvoyerAsync(new ObtenirDirect());

        Assert.Equal(banc.Direct.Direct, direct);
        Assert.Empty(banc.Traces.Traces);
        Assert.Equal(0, banc.UniteDeTravail.Enregistrements);
    }
}
