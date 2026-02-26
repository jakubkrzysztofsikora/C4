using C4.Modules.Graph.Application.GetThreatAssessment;

namespace C4.Modules.Graph.Application.Ports;

public interface IThreatDetector
{
    Task<ThreatDetectionResult> DetectThreatsAsync(Guid projectId, string nodesDescription, string edgesDescription, CancellationToken cancellationToken);
}

public sealed record ThreatDetectionResult(string RiskLevel, IReadOnlyCollection<ThreatItem> Threats);
