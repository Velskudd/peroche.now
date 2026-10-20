using PerocheNow.Domain.Common;
using PerocheNow.Domain.Status;

namespace PerocheNow.Domain.Tests.Status;

public sealed class CurrentStatusTests
{
    private static readonly DateTimeOffset Monday = new(2026, 10, 12, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Tuesday = Monday.AddDays(1);

    [Fact]
    public void Create_trims_the_headline_and_stamps_the_update()
    {
        var status = CurrentStatus.Create("  Je prépare ma première semaine sur la Nordschleife.  ", Monday);

        Assert.Equal("Je prépare ma première semaine sur la Nordschleife.", status.Headline);
        Assert.Equal(Monday, status.UpdatedAt);
        Assert.Empty(status.Lines);
        Assert.Null(status.NextGoal);
        Assert.NotEqual(Guid.Empty, status.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_an_empty_headline(string headline)
    {
        Assert.Throws<BusinessRuleException>(() => CurrentStatus.Create(headline, Monday));
    }

    [Fact]
    public void Create_rejects_a_headline_that_is_too_long()
    {
        var headline = new string('a', CurrentStatus.MaxHeadlineLength + 1);

        Assert.Throws<BusinessRuleException>(() => CurrentStatus.Create(headline, Monday));
    }

    [Fact]
    public void A_headline_at_the_maximum_length_is_accepted()
    {
        var headline = new string('a', CurrentStatus.MaxHeadlineLength);

        var status = CurrentStatus.Create(headline, Monday);

        Assert.Equal(headline, status.Headline);
    }

    [Fact]
    public void ChangeHeadline_replaces_the_text_and_the_date()
    {
        var status = CurrentStatus.Create("Avant", Monday);

        status.ChangeHeadline("Après", Tuesday);

        Assert.Equal("Après", status.Headline);
        Assert.Equal(Tuesday, status.UpdatedAt);
    }

    [Fact]
    public void ReplaceLines_keeps_the_given_order_and_ignores_empty_lines()
    {
        var status = CurrentStatus.Create("Accroche", Monday);

        status.ReplaceLines(["  Première  ", "", "Deuxième", "   "], Tuesday);

        var lines = status.Lines.OrderBy(l => l.Position).ToList();
        Assert.Collection(
            lines,
            l => { Assert.Equal(1, l.Position); Assert.Equal("Première", l.Text); },
            l => { Assert.Equal(2, l.Position); Assert.Equal("Deuxième", l.Text); });
        Assert.Equal(Tuesday, status.UpdatedAt);
    }

    [Fact]
    public void ReplaceLines_replaces_the_previous_lines()
    {
        var status = CurrentStatus.Create("Accroche", Monday);
        status.ReplaceLines(["Une", "Deux", "Trois"], Monday);

        status.ReplaceLines(["Seule"], Tuesday);

        var line = Assert.Single(status.Lines);
        Assert.Equal("Seule", line.Text);
        Assert.Equal(1, line.Position);
    }

    [Fact]
    public void A_line_without_section_leads_nowhere()
    {
        var status = CurrentStatus.Create("Accroche", Monday);

        status.ReplaceLines(["Texte seul"], Tuesday);

        Assert.Equal(Section.None, Assert.Single(status.Lines).Section);
    }

    [Fact]
    public void Each_line_keeps_its_section()
    {
        var status = CurrentStatus.Create("Accroche", Monday);

        status.ReplaceLines([("Sur iRacing", Section.CurrentProject), ("Hors piste", Section.OffTrack)], Tuesday);

        var lines = status.Lines.OrderBy(l => l.Position).ToList();
        Assert.Equal(Section.CurrentProject, lines[0].Section);
        Assert.Equal(Section.OffTrack, lines[1].Section);
    }

    [Fact]
    public void An_unknown_section_is_rejected()
    {
        var status = CurrentStatus.Create("Accroche", Monday);

        Assert.Throws<BusinessRuleException>(() => status.ReplaceLines([("Texte", (Section)42)], Tuesday));
    }

    [Fact]
    public void ReplaceLines_rejects_more_than_three_lines()
    {
        var status = CurrentStatus.Create("Accroche", Monday);

        Assert.Throws<BusinessRuleException>(() => status.ReplaceLines(["1", "2", "3", "4"], Tuesday));
    }

    [Fact]
    public void ReplaceLines_rejects_a_line_that_is_too_long_without_changing_anything()
    {
        var status = CurrentStatus.Create("Accroche", Monday);
        status.ReplaceLines(["Existante"], Monday);

        Assert.Throws<BusinessRuleException>(() =>
            status.ReplaceLines([new string('a', CurrentStatus.MaxLineLength + 1)], Tuesday));

        Assert.Equal("Existante", Assert.Single(status.Lines).Text);
        Assert.Equal(Monday, status.UpdatedAt);
    }

    [Fact]
    public void SetNextGoal_can_set_then_remove_the_goal()
    {
        var status = CurrentStatus.Create("Accroche", Monday);

        status.SetNextGoal(new Goal("Test Week Nordschleife"), Tuesday);
        Assert.Equal("Test Week Nordschleife", status.NextGoal?.Label);

        status.SetNextGoal(null, Tuesday);
        Assert.Null(status.NextGoal);
    }
}
