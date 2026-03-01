namespace C4.Modules.Graph.Application.Ports;

public sealed record ResourceForInference(
    string ExternalResourceId,
    string Name,
    string ServiceType,
    string C4Level,
    string ResourceGroup);
