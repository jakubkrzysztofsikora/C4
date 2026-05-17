namespace C4.Modules.Visualization.Application.Deltas;

public sealed record GraphDeltaNode(
    string Id,
    string Name,
    string Level,
    string? Health,
    string? ServiceType,
    string? Technology,
    string? ResourceGroup,
    string? Environment,
    string? Domain);
