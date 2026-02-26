namespace C4.Modules.Discovery.Domain.Resources;

public sealed record AzureResourceClassification(string FriendlyName, string ServiceType, string C4Level, bool IncludeInDiagram);
