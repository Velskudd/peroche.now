using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PerocheNow.Domain.Status;

namespace PerocheNow.Infrastructure.Persistence.Configurations;

internal sealed class CurrentStatusConfiguration : IEntityTypeConfiguration<CurrentStatus>
{
    public void Configure(EntityTypeBuilder<CurrentStatus> builder)
    {
        builder.ToTable("CurrentStatus");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Headline)
            .HasMaxLength(CurrentStatus.MaxHeadlineLength)
            .IsRequired();

        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.OwnsMany(s => s.Lines, lines =>
        {
            lines.ToTable("StatusLine");
            lines.WithOwner().HasForeignKey("CurrentStatusId");
            lines.Property<int>("Id");
            lines.HasKey("Id");
            lines.Property(l => l.Position).IsRequired();
            lines.Property(l => l.Text)
                .HasMaxLength(CurrentStatus.MaxLineLength)
                .IsRequired();
            // Stockée en clair (« CurrentProject », « Content »…) pour rester lisible en base.
            lines.Property(l => l.Section)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
        });

        // La collection est exposée en lecture seule : EF passe par le champ privé _lines.
        builder.Navigation(s => s.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsOne(s => s.NextGoal, goal =>
        {
            goal.Property(g => g.Label)
                .HasColumnName("GoalLabel")
                .HasMaxLength(Goal.MaxLabelLength)
                .IsRequired();
            goal.Property(g => g.TargetDate).HasColumnName("GoalTargetDate");
            goal.Ignore(g => g.IsDateConfirmed);
        });
    }
}
