using C4.Modules.Visualization.Domain.Preset;
using C4.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace C4.Modules.Visualization.Infrastructure.Persistence;

public sealed class VisualizationDbContext(DbContextOptions<VisualizationDbContext> options) : BaseDbContext(options)
{
    public DbSet<ViewPreset> ViewPresets => Set<ViewPreset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ViewPresetConfiguration());
    }
}

file sealed class ViewPresetConfiguration : IEntityTypeConfiguration<ViewPreset>
{
    public void Configure(EntityTypeBuilder<ViewPreset> builder)
    {
        builder.ToTable("view_presets");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.ProjectId).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Json).IsRequired();
        builder.Property(p => p.CreatedAtUtc).IsRequired();
        builder.HasIndex(p => p.ProjectId);
    }
}
