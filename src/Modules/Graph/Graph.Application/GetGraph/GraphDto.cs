namespace C4.Modules.Graph.Application.GetGraph;

public sealed record GraphDto(
    Guid ProjectId,
    IReadOnlyCollection<GraphNodeDto> Nodes,
    IReadOnlyCollection<GraphEdgeDto> Edges,
    GraphQualityDto? Quality = null);

public sealed record GraphQualityDto(
    int TotalNodes,
    int FallbackClassificationCount,
    int UnknownEnvironmentCount,
    int NonRuntimeNodeCount,
    int RawDeclarationLabelCount);

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
    string GroupKey = "",
    IReadOnlyCollection<string>? Tags = null,
    string? Source = null,
    string EntityKind = "runtime",
    IReadOnlyCollection<string>? DataQualityFlags = null);

public sealed record GraphEdgeDto(
    Guid Id,
    Guid SourceNodeId,
    Guid TargetNodeId,
    double Traffic,
    string TrafficState = "unknown",
    string TrafficLabel = "N/A",
    double? RequestRate = null,
    double? ErrorRate = null,
    double? P95LatencyMs = null,
    string? Protocol = null,
    string? TelemetrySource = null,
    string? TelemetryWindow = null,
    string? SourceExternalResourceId = null,
    string? TargetExternalResourceId = null);
