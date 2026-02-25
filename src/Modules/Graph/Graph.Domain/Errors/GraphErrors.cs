using C4.Shared.Kernel;

namespace C4.Modules.Graph.Domain.Errors;

public static class GraphErrors
{
    public static Error GraphNotFound(Guid projectId) => new("graph.not_found", $"Graph for project '{projectId}' was not found.");
}
