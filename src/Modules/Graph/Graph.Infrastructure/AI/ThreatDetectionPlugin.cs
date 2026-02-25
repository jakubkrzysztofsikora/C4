using C4.Modules.Graph.Application.GetThreatAssessment;
using C4.Modules.Graph.Application.Ports;
using Microsoft.SemanticKernel;

namespace C4.Modules.Graph.Infrastructure.AI;

public sealed class ThreatDetectionPlugin(Kernel kernel) : IThreatDetector
{
    public async Task<ThreatDetectionResult> DetectThreatsAsync(string nodesDescription, string edgesDescription, CancellationToken cancellationToken)
    {
        var prompt = $$"""
            Analyze the following architecture for security threats using STRIDE methodology.

            Nodes: {{nodesDescription}}
            Edges: {{edgesDescription}}

            For each threat found, provide:
            - Component affected
            - Threat type (Spoofing, Tampering, Repudiation, Information Disclosure, Denial of Service, Elevation of Privilege)
            - Severity (Low, Medium, High, Critical)
            - Recommended mitigation

            Format your response as:
            RISK_LEVEL: <Low|Medium|High|Critical>
            THREATS:
            - COMPONENT: <name> | TYPE: <type> | SEVERITY: <severity> | MITIGATION: <mitigation>
            """;

        var result = await kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        var text = result.GetValue<string>() ?? string.Empty;

        return ParseThreatResult(text);
    }

    private static ThreatDetectionResult ParseThreatResult(string text)
    {
        var riskLevel = "Low";
        var threats = new List<ThreatItem>();

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("RISK_LEVEL:", StringComparison.OrdinalIgnoreCase))
            {
                riskLevel = trimmed["RISK_LEVEL:".Length..].Trim();
            }
            else if (trimmed.StartsWith("- COMPONENT:", StringComparison.OrdinalIgnoreCase))
            {
                var threat = ParseThreatLine(trimmed);
                if (threat is not null) threats.Add(threat);
            }
        }

        return new ThreatDetectionResult(riskLevel, threats);
    }

    private static ThreatItem? ParseThreatLine(string line)
    {
        var parts = line.Split('|');
        if (parts.Length < 4) return null;

        static string ExtractValue(string part)
        {
            var colonIndex = part.IndexOf(':');
            return colonIndex >= 0 ? part[(colonIndex + 1)..].Trim() : part.Trim();
        }

        return new ThreatItem(
            ExtractValue(parts[0]),
            ExtractValue(parts[1]),
            ExtractValue(parts[2]),
            ExtractValue(parts[3]));
    }
}
