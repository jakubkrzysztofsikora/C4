using C4.Modules.Discovery.Domain.Resources;
using C4.Modules.Discovery.Domain.Subscriptions;
using C4.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace C4.Modules.Discovery.Infrastructure.Persistence;

public sealed class DiscoveryDbContext(DbContextOptions<DiscoveryDbContext> options) : BaseDbContext(options)
{
    public DbSet<AzureSubscription> Subscriptions => Set<AzureSubscription>();
    public DbSet<DiscoveredResource> Resources => Set<DiscoveredResource>();
    public DbSet<DriftItemEntity> DriftItems => Set<DriftItemEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AzureSubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new DiscoveredResourceConfiguration());
        modelBuilder.ApplyConfiguration(new DriftItemEntityConfiguration());
    }
}

public sealed class DriftItemEntity
{
    public Guid Id { get; init; }
    public Guid SubscriptionId { get; init; }
    public string ResourceId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

file sealed class AzureSubscriptionConfiguration : IEntityTypeConfiguration<AzureSubscription>
{
    public void Configure(EntityTypeBuilder<AzureSubscription> builder)
    {
        builder.ToTable("azure_subscriptions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => new AzureSubscriptionId(value))
            .ValueGeneratedNever();
        builder.Property(s => s.ExternalSubscriptionId).HasMaxLength(200).IsRequired();
        builder.Property(s => s.DisplayName).HasMaxLength(250).IsRequired();
        builder.Property(s => s.ConnectedAtUtc).IsRequired();
        builder.HasIndex(s => s.ExternalSubscriptionId).IsUnique();
        builder.Ignore(s => s.DomainEvents);
    }
}

file sealed class DiscoveredResourceConfiguration : IEntityTypeConfiguration<DiscoveredResource>
{
    public void Configure(EntityTypeBuilder<DiscoveredResource> builder)
    {
        builder.ToTable("discovered_resources");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new DiscoveredResourceId(value))
            .ValueGeneratedNever();
        builder.Property(r => r.ResourceId).HasMaxLength(500).IsRequired();
        builder.Property(r => r.ResourceType).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(250).IsRequired();
        builder.Property<Guid>("SubscriptionId");
        builder.HasIndex("SubscriptionId", "ResourceId").IsUnique();
        builder.OwnsOne(r => r.Classification, classification =>
        {
            classification.Property(c => c.FriendlyName).HasMaxLength(250);
            classification.Property(c => c.ServiceType).HasMaxLength(100);
            classification.Property(c => c.C4Level).HasMaxLength(50);
        });
    }
}

file sealed class DriftItemEntityConfiguration : IEntityTypeConfiguration<DriftItemEntity>
{
    public void Configure(EntityTypeBuilder<DriftItemEntity> builder)
    {
        builder.ToTable("drift_items");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.ResourceId).HasMaxLength(500).IsRequired();
        builder.Property(d => d.Status).HasMaxLength(50).IsRequired();
        builder.HasIndex(d => d.SubscriptionId);
    }
}
