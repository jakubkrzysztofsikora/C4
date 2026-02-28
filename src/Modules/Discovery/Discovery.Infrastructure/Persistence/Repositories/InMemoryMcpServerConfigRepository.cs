using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.McpServers;

namespace C4.Modules.Discovery.Infrastructure.Persistence.Repositories;

public sealed class InMemoryMcpServerConfigRepository : IMcpServerConfigRepository
{
    private readonly List<McpServerConfig> _configs = [];

    public Task<IReadOnlyCollection<McpServerConfig>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<McpServerConfig>>(_configs.Where(c => c.ProjectId == projectId).ToList());

    public Task<McpServerConfig?> GetByIdAsync(McpServerConfigId id, CancellationToken cancellationToken) =>
        Task.FromResult(_configs.FirstOrDefault(c => c.Id == id));

    public Task AddAsync(McpServerConfig config, CancellationToken cancellationToken)
    {
        _configs.Add(config);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(McpServerConfigId id, CancellationToken cancellationToken)
    {
        McpServerConfig? existing = _configs.FirstOrDefault(c => c.Id == id);
        if (existing is not null)
        {
            _configs.Remove(existing);
        }
        return Task.CompletedTask;
    }
}
