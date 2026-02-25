using C4.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace C4.Modules.Telemetry.Infrastructure.Persistence;

public sealed class TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : BaseDbContext(options)
{
    public DbSet<TelemetryMetricEntity> Metrics => Set<TelemetryMetricEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TelemetryMetricConfiguration());
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
