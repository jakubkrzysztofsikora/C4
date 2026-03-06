using C4.Modules.Discovery.Domain.McpServers;
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
    public DbSet<AzureTokenEntity> AzureTokens => Set<AzureTokenEntity>();
    public DbSet<McpServerConfig> McpServerConfigs => Set<McpServerConfig>();
    public DbSet<ProjectArchitectureProfileEntity> ProjectArchitectureProfiles => Set<ProjectArchitectureProfileEntity>();
    public DbSet<ProjectArchitectureQuestionEntity> ProjectArchitectureQuestions => Set<ProjectArchitectureQuestionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AzureSubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new DiscoveredResourceConfiguration());
        modelBuilder.ApplyConfiguration(new DriftItemEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AzureTokenEntityConfiguration());
        modelBuilder.ApplyConfiguration(new McpServerConfigConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectArchitectureProfileConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectArchitectureQuestionConfiguration());
    }
}

public sealed class AzureTokenEntity
{
    public string ExternalSubscriptionId { get; init; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class DriftItemEntity
{
    public Guid Id { get; init; }
    public Guid SubscriptionId { get; init; }
    public string ResourceId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public sealed class ProjectArchitectureProfileEntity
{
    public Guid ProjectId { get; init; }
    public string ProjectDescription { get; set; } = string.Empty;
    public string SystemBoundaries { get; set; } = string.Empty;
    public string CoreDomains { get; set; } = string.Empty;
    public string ExternalDependencies { get; set; } = string.Empty;
    public string DataSensitivity { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public DateTime LastUpdatedAtUtc { get; set; }
    public DateTime? LastQuestionGenerationAtUtc { get; set; }
    public int? LastResourceCount { get; set; }
}

public sealed class ProjectArchitectureQuestionEntity
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string Question { get; set; } = string.Empty;
    public string? Answer { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? AnsweredAtUtc { get; set; }
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
        builder.Property(s => s.GitRepoUrl).HasColumnType("text");
        builder.Property(s => s.GitPatToken).HasMaxLength(500);
        builder.Property(s => s.GitBranch).HasMaxLength(200);
        builder.Property(s => s.GitRootPath).HasMaxLength(500);
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
            classification.Property(c => c.ClassificationSource).HasMaxLength(50);
            classification.Property(c => c.Confidence);
            classification.Property(c => c.IsInfrastructure);
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

file sealed class AzureTokenEntityConfiguration : IEntityTypeConfiguration<AzureTokenEntity>
{
    public void Configure(EntityTypeBuilder<AzureTokenEntity> builder)
    {
        builder.ToTable("azure_tokens");
        builder.HasKey(t => t.ExternalSubscriptionId);
        builder.Property(t => t.ExternalSubscriptionId).HasColumnName("external_subscription_id").HasMaxLength(200);
        builder.Property(t => t.AccessToken).HasColumnName("access_token").HasColumnType("text").IsRequired();
        builder.Property(t => t.RefreshToken).HasColumnName("refresh_token").HasColumnType("text");
        builder.Property(t => t.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
    }
}

file sealed class McpServerConfigConfiguration : IEntityTypeConfiguration<McpServerConfig>
{
    public void Configure(EntityTypeBuilder<McpServerConfig> builder)
    {
        builder.ToTable("mcp_server_configs");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new McpServerConfigId(value))
            .ValueGeneratedNever();
        builder.Property(c => c.ProjectId).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Endpoint).HasMaxLength(500).IsRequired();
        builder.Property(c => c.AuthMode).HasMaxLength(50).IsRequired();
        builder.Property(c => c.CreatedAtUtc).IsRequired();
        builder.HasIndex(c => c.ProjectId);
    }
}

file sealed class ProjectArchitectureProfileConfiguration : IEntityTypeConfiguration<ProjectArchitectureProfileEntity>
{
    public void Configure(EntityTypeBuilder<ProjectArchitectureProfileEntity> builder)
    {
        builder.ToTable("project_architecture_profiles");
        builder.HasKey(p => p.ProjectId);
        builder.Property(p => p.ProjectDescription).HasColumnType("text").IsRequired();
        builder.Property(p => p.SystemBoundaries).HasColumnType("text").IsRequired();
        builder.Property(p => p.CoreDomains).HasColumnType("text").IsRequired();
        builder.Property(p => p.ExternalDependencies).HasColumnType("text").IsRequired();
        builder.Property(p => p.DataSensitivity).HasColumnType("text").IsRequired();
        builder.Property(p => p.IsApproved).IsRequired();
        builder.Property(p => p.LastUpdatedAtUtc).IsRequired();
        builder.Property(p => p.LastQuestionGenerationAtUtc);
        builder.Property(p => p.LastResourceCount);
    }
}

file sealed class ProjectArchitectureQuestionConfiguration : IEntityTypeConfiguration<ProjectArchitectureQuestionEntity>
{
    public void Configure(EntityTypeBuilder<ProjectArchitectureQuestionEntity> builder)
    {
        builder.ToTable("project_architecture_questions");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.ProjectId).IsRequired();
        builder.Property(q => q.Question).HasColumnType("text").IsRequired();
        builder.Property(q => q.Answer).HasColumnType("text");
        builder.Property(q => q.IsApproved).IsRequired();
        builder.Property(q => q.CreatedAtUtc).IsRequired();
        builder.Property(q => q.AnsweredAtUtc);
        builder.HasIndex(q => q.ProjectId);
    }
}
