using PerocheNow.Domain.Audit;

namespace PerocheNow.Domain.Tests.Audit;

public sealed class AuditEntryTests
{
    [Fact]
    public void An_entry_keeps_everything_it_is_given()
    {
        var occurredAt = new DateTimeOffset(2026, 10, 17, 13, 32, 0, TimeSpan.Zero);
        var objectId = Guid.NewGuid();

        var entry = new AuditEntry("SetCurrentStatus", "alexandre", occurredAt, "CurrentStatus", objectId, "détails");

        Assert.Equal("SetCurrentStatus", entry.Action);
        Assert.Equal("alexandre", entry.Author);
        Assert.Equal(occurredAt, entry.OccurredAt);
        Assert.Equal("CurrentStatus", entry.ObjectType);
        Assert.Equal(objectId, entry.ObjectId);
        Assert.Equal("détails", entry.Details);
    }
}
