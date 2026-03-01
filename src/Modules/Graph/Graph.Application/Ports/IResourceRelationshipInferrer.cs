namespace C4.Modules.Graph.Application.Ports;

public interface IResourceRelationshipInferrer
{
    Task<IReadOnlyCollection<InferredRelationship>> InferRelationshipsAsync(
        Guid projectId,
        IReadOnlyCollection<ResourceForInference> resources,
        IReadOnlyCollection<string> existingEdgeDescriptions,
        CancellationToken cancellationToken);
}
