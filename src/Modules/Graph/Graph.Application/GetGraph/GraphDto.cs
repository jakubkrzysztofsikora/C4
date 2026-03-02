namespace C4.Modules.Graph.Application.GetGraph;

public sealed record GraphDto(Guid ProjectId, IReadOnlyCollection<GraphNodeDto> Nodes, IReadOnlyCollection<GraphEdgeDto> Edges);
public sealed record GraphNodeDto(
    Guid Id,
    string Name,
    string ExternalResourceId,
    string Level,
    string Health,
    double HealthScore,
    string TelemetryStatus,
    double? RequestRate,
    double? ErrorRate,
    double? P95LatencyMs,
    string? RiskLevel,
    double? HourlyCostUsd,
    Guid? ParentNodeId,
    bool Drift = false,
    string Environment = "unknown",
    string ServiceType = "external",
    string Technology = "unknown",
    string ResourceGroup = "",
    string Domain = "General",
    bool IsInfrastructure = false,
    string ClassificationSource = "fallback",
    double ClassificationConfidence = 0.6,
    string GroupKey = "");
public sealed record GraphEdgeDto(
    Guid Id,
    Guid SourceNodeId,
    Guid TargetNodeId,
    double Traffic,
    string TrafficState = "unknown",
    double? RequestRate = null,
    double? ErrorRate = null,
    double? P95LatencyMs = null,
    string? Protocol = null);
