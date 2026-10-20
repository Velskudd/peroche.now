using PerocheNow.Domain.Common;
using PerocheNow.Domain.Status;

namespace PerocheNow.Domain.Tests.Status;

public sealed class GoalTests
{
    [Fact]
    public void Without_a_date_the_goal_is_not_confirmed()
    {
        var goal = new Goal("  Test Week Nordschleife ");

        Assert.Equal("Test Week Nordschleife", goal.Label);
        Assert.False(goal.IsDateConfirmed);
    }

    [Fact]
    public void With_a_date_the_goal_is_confirmed()
    {
        var goal = new Goal("Test Week Nordschleife", new DateOnly(2027, 3, 15));

        Assert.True(goal.IsDateConfirmed);
        Assert.Equal(new DateOnly(2027, 3, 15), goal.TargetDate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void An_empty_label_is_rejected(string label)
    {
        Assert.Throws<BusinessRuleException>(() => new Goal(label));
    }

    [Fact]
    public void A_label_that_is_too_long_is_rejected()
    {
        Assert.Throws<BusinessRuleException>(() => new Goal(new string('a', Goal.MaxLabelLength + 1)));
    }
}
