namespace C4.Modules.Graph.Domain.GraphNode;

public sealed record NodeProperties(
    string Technology,
    string Owner,
    IReadOnlyCollection<string> Tags,
    decimal Cost,
    string Domain = "General",
    bool IsInfrastructure = false,
    string ClassificationSource = "fallback",
    double ClassificationConfidence = 0.6);
