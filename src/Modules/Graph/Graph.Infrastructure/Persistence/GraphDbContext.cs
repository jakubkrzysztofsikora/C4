using C4.Modules.Graph.Domain.ArchitectureGraph;
using C4.Modules.Graph.Domain.GraphEdge;
using C4.Modules.Graph.Domain.GraphNode;
using C4.Modules.Graph.Domain.GraphSnapshot;
using C4.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace C4.Modules.Graph.Infrastructure.Persistence;

public sealed class GraphDbContext(DbContextOptions<GraphDbContext> options) : BaseDbContext(options)
{
    public DbSet<ArchitectureGraph> Graphs => Set<ArchitectureGraph>();
    public DbSet<GraphNode> Nodes => Set<GraphNode>();
    public DbSet<GraphEdge> Edges => Set<GraphEdge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ArchitectureGraphConfiguration());
        modelBuilder.ApplyConfiguration(new GraphNodeConfiguration());
        modelBuilder.ApplyConfiguration(new GraphEdgeConfiguration());
        modelBuilder.ApplyConfiguration(new GraphSnapshotConfiguration());
    }
}

file sealed class ArchitectureGraphConfiguration : IEntityTypeConfiguration<ArchitectureGraph>
{
    public void Configure(EntityTypeBuilder<ArchitectureGraph> builder)
    {
        builder.ToTable("architecture_graphs");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id)
            .HasConversion(id => id.Value, value => new ArchitectureGraphId(value))
            .ValueGeneratedNever();
        builder.Property(g => g.ProjectId).IsRequired();
        builder.HasIndex(g => g.ProjectId).IsUnique();
        builder.HasMany(g => g.Nodes).WithOne().HasForeignKey("ArchitectureGraphId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(g => g.Edges).WithOne().HasForeignKey("ArchitectureGraphId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(g => g.Snapshots).WithOne().HasForeignKey("ArchitectureGraphId").OnDelete(DeleteBehavior.Cascade);
        builder.Ignore(g => g.DomainEvents);
    }
}

file sealed class GraphNodeConfiguration : IEntityTypeConfiguration<GraphNode>
{
    public void Configure(EntityTypeBuilder<GraphNode> builder)
    {
        builder.ToTable("graph_nodes");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id)
            .HasConversion(id => id.Value, value => new GraphNodeId(value))
            .ValueGeneratedNever();
        builder.Property(n => n.ExternalResourceId).HasMaxLength(500).IsRequired();
        builder.Property(n => n.Name).HasMaxLength(250).IsRequired();
        builder.Property(n => n.Level).HasConversion<int>().IsRequired();
        builder.Property(n => n.ParentId)
            .HasConversion(id => id == null ? (Guid?)null : id.Value, value => value == null ? null : new GraphNodeId(value.Value));
        builder.HasIndex("ExternalResourceId", "ArchitectureGraphId").IsUnique();
        builder.OwnsOne(n => n.Properties, props =>
        {
            props.Property(p => p.Technology).HasMaxLength(200).HasColumnName("technology");
            props.Property(p => p.Owner).HasMaxLength(200).HasColumnName("owner");
            props.Property(p => p.Cost).HasColumnName("cost");
            props.Property(p => p.Tags).HasColumnName("tags")
                .HasConversion(
                    v => string.Join(",", v),
                    v => v.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList().AsReadOnly());
        });
    }
}

file sealed class GraphEdgeConfiguration : IEntityTypeConfiguration<GraphEdge>
{
    public void Configure(EntityTypeBuilder<GraphEdge> builder)
    {
        builder.ToTable("graph_edges");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new GraphEdgeId(value))
            .ValueGeneratedNever();
        builder.Property(e => e.SourceNodeId)
            .HasConversion(id => id.Value, value => new GraphNodeId(value));
        builder.Property(e => e.TargetNodeId)
            .HasConversion(id => id.Value, value => new GraphNodeId(value));
        builder.OwnsOne(e => e.Properties, props =>
        {
            props.Property(p => p.Protocol).HasMaxLength(50).HasColumnName("protocol");
            props.Property(p => p.Port).HasColumnName("port");
            props.Property(p => p.Direction).HasMaxLength(50).HasColumnName("direction");
        });
    }
}

file sealed class GraphSnapshotConfiguration : IEntityTypeConfiguration<GraphSnapshot>
{
    public void Configure(EntityTypeBuilder<GraphSnapshot> builder)
    {
        builder.ToTable("graph_snapshots");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => new GraphSnapshotId(value))
            .ValueGeneratedNever();
        builder.Property(s => s.CreatedAtUtc).IsRequired();
        builder.Ignore(s => s.Nodes);
        builder.Ignore(s => s.Edges);
    }
}
