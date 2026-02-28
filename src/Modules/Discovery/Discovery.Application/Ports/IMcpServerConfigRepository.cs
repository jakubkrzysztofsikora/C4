using C4.Modules.Discovery.Domain.McpServers;

namespace C4.Modules.Discovery.Application.Ports;

public interface IMcpServerConfigRepository
{
    Task<IReadOnlyCollection<McpServerConfig>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken);
    Task<McpServerConfig?> GetByIdAsync(McpServerConfigId id, CancellationToken cancellationToken);
    Task AddAsync(McpServerConfig config, CancellationToken cancellationToken);
    Task DeleteAsync(McpServerConfigId id, CancellationToken cancellationToken);
}
