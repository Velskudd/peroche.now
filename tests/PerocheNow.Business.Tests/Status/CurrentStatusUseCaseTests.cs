using PerocheNow.Business.Status;
using PerocheNow.Domain.Common;
using PerocheNow.Domain.Status;

namespace PerocheNow.Business.Tests.Status;

public sealed class CurrentStatusUseCaseTests
{
    private static SetCurrentStatus Command(params string[] lines) =>
        new("Je prépare ma première semaine sur la Nordschleife.",
            lines.Select(l => new LineInput(l)).ToList(),
            "Test Week Nordschleife",
            null);

    [Fact]
    public async Task Set_creates_the_status_when_none_exists()
    {
        using var bench = new TestBench();
        using var scope = bench.NewScope();

        var id = await TestBench.Mediator(scope).SendAsync(Command("Une", "Deux"));

        var status = Assert.IsType<CurrentStatus>(bench.Statuses.Status);
        Assert.Equal(id, status.Id);
        Assert.Equal(2, status.Lines.Count);
        Assert.Equal("Test Week Nordschleife", status.NextGoal?.Label);
        Assert.Equal(TestBench.Now, status.UpdatedAt);
    }

    [Fact]
    public async Task Set_saves_once_together_with_its_audit_entry()
    {
        using var bench = new TestBench();
        using var scope = bench.NewScope();

        var id = await TestBench.Mediator(scope).SendAsync(Command("Une"));

        Assert.Equal(1, bench.UnitOfWork.SaveCount);
        var entry = Assert.Single(bench.AuditEntries.Entries);
        Assert.Equal(nameof(SetCurrentStatus), entry.Action);
        Assert.Equal("alexandre", entry.Author);
        Assert.Equal(id, entry.ObjectId);
        Assert.Equal(TestBench.Now, entry.OccurredAt);
    }

    [Fact]
    public async Task Set_updates_the_existing_status_without_creating_another()
    {
        using var bench = new TestBench();
        var existing = CurrentStatus.Create("Avant", TestBench.Now.AddDays(-3));
        bench.Statuses.Status = existing;
        using var scope = bench.NewScope();

        var id = await TestBench.Mediator(scope).SendAsync(
            new SetCurrentStatus("Après", [new LineInput("Seule ligne", Section.Content)], Goal: null, GoalDate: null));

        Assert.Equal(existing.Id, id);
        Assert.Same(existing, bench.Statuses.Status);
        Assert.Equal("Après", existing.Headline);
        Assert.Equal(Section.Content, Assert.Single(existing.Lines).Section);
        Assert.Null(existing.NextGoal);
        Assert.Equal(TestBench.Now, existing.UpdatedAt);
    }

    [Fact]
    public async Task A_broken_business_rule_saves_nothing_and_records_nothing()
    {
        using var bench = new TestBench();
        using var scope = bench.NewScope();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            TestBench.Mediator(scope).SendAsync(Command("1", "2", "3", "4")));

        Assert.Equal(0, bench.UnitOfWork.SaveCount);
        Assert.Empty(bench.AuditEntries.Entries);
    }

    [Fact]
    public async Task Get_returns_null_until_a_status_is_set()
    {
        using var bench = new TestBench();
        using var scope = bench.NewScope();

        var status = await TestBench.Mediator(scope).SendAsync(new GetCurrentStatus());

        Assert.Null(status);
    }

    [Fact]
    public async Task Get_returns_the_lines_in_order_without_recording_or_saving()
    {
        using var bench = new TestBench();
        var status = CurrentStatus.Create("Accroche", TestBench.Now);
        status.ReplaceLines(
            [("Première", Section.CurrentProject), ("Deuxième", Section.None), ("Troisième", Section.OffTrack)],
            TestBench.Now);
        status.SetNextGoal(new Goal("Test Week Nordschleife", new DateOnly(2027, 3, 15)), TestBench.Now);
        bench.Statuses.Status = status;
        using var scope = bench.NewScope();

        var dto = await TestBench.Mediator(scope).SendAsync(new GetCurrentStatus());

        Assert.NotNull(dto);
        Assert.Equal("Accroche", dto.Headline);
        StatusLineDto[] expectedLines =
        [
            new("Première", Section.CurrentProject),
            new("Deuxième", Section.None),
            new("Troisième", Section.OffTrack),
        ];
        Assert.Equal(expectedLines, dto.Lines);
        Assert.Equal(new GoalDto("Test Week Nordschleife", new DateOnly(2027, 3, 15)), dto.NextGoal);
        Assert.Equal(0, bench.UnitOfWork.SaveCount);
        Assert.Empty(bench.AuditEntries.Entries);
    }

    [Fact]
    public void The_fallback_status_mirrors_the_initial_status()
    {
        var fallback = InitialStatus.AsFallbackStatus(TestBench.Now);

        Assert.Equal(InitialStatus.Command.Headline, fallback.Headline);
        Assert.Equal(
            InitialStatus.Command.Lines.Select(l => new StatusLineDto(l.Text, l.Section)),
            fallback.Lines);
        Assert.Equal(InitialStatus.Command.Goal, fallback.NextGoal?.Label);
    }

    [Fact]
    public async Task The_initial_status_follows_the_domain_rules()
    {
        using var bench = new TestBench();
        using var scope = bench.NewScope();

        await TestBench.Mediator(scope).SendAsync(InitialStatus.Command);

        Assert.NotNull(bench.Statuses.Status);
    }
}
