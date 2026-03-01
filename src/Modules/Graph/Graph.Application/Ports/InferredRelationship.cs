namespace C4.Modules.Graph.Application.Ports;

public sealed record InferredRelationship(
    string SourceResourceId,
    string TargetResourceId,
    double Confidence);
