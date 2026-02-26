using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Modules.Feedback.Domain.Learning;
using C4.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace C4.Modules.Feedback.Infrastructure.Persistence;

public sealed class FeedbackDbContext(DbContextOptions<FeedbackDbContext> options) : BaseDbContext(options)
{
    public DbSet<FeedbackEntry> FeedbackEntries => Set<FeedbackEntry>();
    public DbSet<LearningInsight> LearningInsights => Set<LearningInsight>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new FeedbackEntryConfiguration());
        modelBuilder.ApplyConfiguration(new LearningInsightConfiguration());
    }
}

file sealed class FeedbackEntryConfiguration : IEntityTypeConfiguration<FeedbackEntry>
{
    public void Configure(EntityTypeBuilder<FeedbackEntry> builder)
    {
        builder.ToTable("feedback_entries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new FeedbackEntryId(value))
            .ValueGeneratedNever();
        builder.Property(e => e.UserId).IsRequired();
        builder.OwnsOne(e => e.Target, target =>
        {
            target.Property(t => t.TargetType).HasConversion<int>().HasColumnName("target_type").IsRequired();
            target.Property(t => t.TargetId).HasColumnName("target_id").IsRequired();
        });
        builder.Property(e => e.Category).HasConversion<int>().IsRequired();
        builder.OwnsOne(e => e.Rating, rating =>
        {
            rating.Property(r => r.Score).HasColumnName("rating").IsRequired();
        });
        builder.Property(e => e.Comment).HasMaxLength(2000);
        builder.OwnsOne(e => e.NodeCorrection, nc =>
        {
            nc.ToJson("node_correction");
        });
        builder.OwnsOne(e => e.EdgeCorrection, ec =>
        {
            ec.ToJson("edge_correction");
        });
        builder.OwnsOne(e => e.ClassificationCorrection, cc =>
        {
            cc.ToJson("classification_correction");
        });
        builder.Property(e => e.SubmittedAtUtc).IsRequired();
        builder.HasIndex(e => new { e.Category, e.SubmittedAtUtc });
        builder.Ignore(e => e.DomainEvents);
    }
}

file sealed class LearningInsightConfiguration : IEntityTypeConfiguration<LearningInsight>
{
    public void Configure(EntityTypeBuilder<LearningInsight> builder)
    {
        builder.ToTable("learning_insights");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .HasConversion(id => id.Value, value => new LearningInsightId(value))
            .ValueGeneratedNever();
        builder.Property(i => i.ProjectId).IsRequired();
        builder.Property(i => i.Category).HasConversion<int>().IsRequired();
        builder.Property(i => i.InsightType).HasConversion<int>().IsRequired();
        builder.Property(i => i.Description).HasMaxLength(2000).IsRequired();
        builder.Property(i => i.Confidence).IsRequired();
        builder.Property(i => i.FeedbackCount).IsRequired();
        builder.Property(i => i.CreatedAtUtc).IsRequired();
        builder.Property(i => i.ExpiresAtUtc).IsRequired();
        builder.HasIndex(i => new { i.ProjectId, i.Category });
        builder.HasIndex(i => i.ExpiresAtUtc);
        builder.Ignore(i => i.DomainEvents);
    }
}
