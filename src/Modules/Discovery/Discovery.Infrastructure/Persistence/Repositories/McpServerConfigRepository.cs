using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.McpServers;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Discovery.Infrastructure.Persistence.Repositories;

public sealed class McpServerConfigRepository(DiscoveryDbContext dbContext) : IMcpServerConfigRepository
{
    public async Task<IReadOnlyCollection<McpServerConfig>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.McpServerConfigs
            .Where(c => c.ProjectId == projectId)
            .ToListAsync(cancellationToken);

    public async Task<McpServerConfig?> GetByIdAsync(McpServerConfigId id, CancellationToken cancellationToken) =>
        await dbContext.McpServerConfigs
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(McpServerConfig config, CancellationToken cancellationToken) =>
        await dbContext.McpServerConfigs.AddAsync(config, cancellationToken);

    public Task DeleteAsync(McpServerConfigId id, CancellationToken cancellationToken)
    {
        dbContext.McpServerConfigs.Where(c => c.Id == id).ExecuteDelete();
        return Task.CompletedTask;
    }
}
