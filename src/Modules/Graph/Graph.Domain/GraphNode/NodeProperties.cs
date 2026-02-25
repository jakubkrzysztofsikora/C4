namespace C4.Modules.Graph.Domain.GraphNode;

public sealed record NodeProperties(string Technology, string Owner, IReadOnlyCollection<string> Tags, decimal Cost);
