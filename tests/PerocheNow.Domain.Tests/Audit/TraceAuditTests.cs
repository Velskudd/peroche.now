using PerocheNow.Domain.Audit;

namespace PerocheNow.Domain.Tests.Audit;

public sealed class TraceAuditTests
{
    [Fact]
    public void Une_trace_garde_tout_ce_qu_on_lui_donne()
    {
        var le = new DateTimeOffset(2026, 10, 17, 13, 32, 0, TimeSpan.Zero);
        var idObjet = Guid.NewGuid();

        var trace = new TraceAudit("DefinirStatutDuMoment", "alexandre", le, "StatutDuMoment", idObjet, "détails");

        Assert.Equal("DefinirStatutDuMoment", trace.Action);
        Assert.Equal("alexandre", trace.Auteur);
        Assert.Equal(le, trace.Le);
        Assert.Equal("StatutDuMoment", trace.TypeObjet);
        Assert.Equal(idObjet, trace.IdObjet);
        Assert.Equal("détails", trace.Details);
    }
}
