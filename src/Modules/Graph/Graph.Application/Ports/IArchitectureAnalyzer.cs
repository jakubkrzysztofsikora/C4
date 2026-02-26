namespace C4.Modules.Graph.Application.Ports;

public interface IArchitectureAnalyzer
{
    Task<ArchitectureAnalysisResult> AnalyzeAsync(Guid projectId, string nodesDescription, string edgesDescription, CancellationToken cancellationToken);
}

public sealed record ArchitectureAnalysisResult(string Summary, IReadOnlyCollection<string> Recommendations);
