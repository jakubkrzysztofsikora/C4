namespace C4.Modules.Graph.Application.GetGraph;

public sealed record GraphDto(Guid ProjectId, IReadOnlyCollection<GraphNodeDto> Nodes, IReadOnlyCollection<GraphEdgeDto> Edges);
public sealed record GraphNodeDto(Guid Id, string Name, string ExternalResourceId, string Level);
public sealed record GraphEdgeDto(Guid Id, Guid SourceNodeId, Guid TargetNodeId);
