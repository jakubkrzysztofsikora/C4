using C4.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace C4.Modules.Telemetry.Infrastructure.Persistence;

public sealed class TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : BaseDbContext(options)
{
    public DbSet<TelemetryMetricEntity> Metrics => Set<TelemetryMetricEntity>();
    public DbSet<AppInsightsConfigEntity> AppInsightsConfigs => Set<AppInsightsConfigEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TelemetryMetricConfiguration());
        modelBuilder.ApplyConfiguration(new AppInsightsConfigEntityConfiguration());
    }
}

public sealed class TelemetryMetricEntity
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string Service { get; init; } = string.Empty;
    public double Value { get; init; }
    public DateTime TimestampUtc { get; init; }
}

public sealed class AppInsightsConfigEntity
{
    public Guid ProjectId { get; init; }
    public string AppId { get; set; } = string.Empty;
    public string InstrumentationKey { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
}

file sealed class TelemetryMetricConfiguration : IEntityTypeConfiguration<TelemetryMetricEntity>
{
    public void Configure(EntityTypeBuilder<TelemetryMetricEntity> builder)
    {
        builder.ToTable("telemetry_metrics");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Service).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Value).IsRequired();
        builder.Property(x => x.TimestampUtc).IsRequired();
        builder.HasIndex(x => new { x.ProjectId, x.Service, x.TimestampUtc });
    }
}

file sealed class AppInsightsConfigEntityConfiguration : IEntityTypeConfiguration<AppInsightsConfigEntity>
{
    public void Configure(EntityTypeBuilder<AppInsightsConfigEntity> builder)
    {
        builder.ToTable("app_insights_configs");
        builder.HasKey(x => x.ProjectId);
        builder.Property(x => x.AppId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.InstrumentationKey).HasMaxLength(200);
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
    }
}
