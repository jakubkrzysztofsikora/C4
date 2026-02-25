using C4.Modules.Visualization.Application.Ports;

namespace C4.Modules.Visualization.Api.Persistence;

public sealed class InMemoryDiagramReadModel : IDiagramReadModel
{
    public Task<string?> GetDiagramJsonAsync(Guid projectId, CancellationToken cancellationToken)
        => Task.FromResult<string?>($"{{\"projectId\":\"{projectId}\",\"nodes\":[],\"edges\":[]}}");
}
