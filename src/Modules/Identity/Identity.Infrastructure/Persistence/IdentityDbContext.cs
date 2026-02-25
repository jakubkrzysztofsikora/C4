using C4.Modules.Identity.Domain.Member;
using C4.Modules.Identity.Domain.Organization;
using C4.Modules.Identity.Domain.Project;
using C4.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace C4.Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : BaseDbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Member> Members => Set<Member>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OrganizationConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectConfiguration());
        modelBuilder.ApplyConfiguration(new MemberConfiguration());
    }
}

file sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasConversion(id => id.Value, value => new OrganizationId(value))
            .ValueGeneratedNever();
        builder.Property(o => o.Name).HasMaxLength(150).IsRequired();
        builder.Ignore(o => o.Projects);
        builder.Ignore(o => o.DomainEvents);
    }
}

file sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new ProjectId(value))
            .ValueGeneratedNever();
        builder.Property(p => p.OrganizationId)
            .HasConversion(id => id.Value, value => new OrganizationId(value));
        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
        builder.HasIndex(p => new { p.OrganizationId, p.Name }).IsUnique();
        builder.Ignore(p => p.DomainEvents);
    }
}

file sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("members");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasConversion(id => id.Value, value => new MemberId(value))
            .ValueGeneratedNever();
        builder.Property(m => m.ProjectId)
            .HasConversion(id => id.Value, value => new ProjectId(value));
        builder.Property(m => m.ExternalUserId).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Role).HasConversion<int>().IsRequired();
        builder.HasIndex(m => new { m.ProjectId, m.ExternalUserId }).IsUnique();
    }
}
