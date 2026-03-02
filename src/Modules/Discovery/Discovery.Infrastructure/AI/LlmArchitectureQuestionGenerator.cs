using C4.Modules.Discovery.Application.Ports;
using Microsoft.SemanticKernel;

namespace C4.Modules.Discovery.Infrastructure.AI;

public sealed class LlmArchitectureQuestionGenerator(Kernel kernel) : IArchitectureQuestionGenerator
{
    public async Task<IReadOnlyCollection<string>> GenerateQuestionsAsync(Guid projectId, string contextSummary, CancellationToken cancellationToken)
    {
        var prompt = $"""
            You are a senior architecture consultant. Generate clarifying questions for an Azure architecture model.
            The goal is to improve C4 diagram accuracy and threat modeling quality.

            Existing project context:
            {contextSummary}

            Return only questions, one per line, no numbering.
            Questions must help identify:
            - system boundaries
            - core services and domains
            - data flows and trust boundaries
            - security/compliance constraints
            - external integrations
            """;

        try
        {
            var result = await kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var text = result.GetValue<string>() ?? string.Empty;
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim().TrimStart('-', '*', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', ')'))
                .Where(line => line.EndsWith("?", StringComparison.Ordinal))
                .ToArray();
            if (lines.Length > 0)
                return lines;
        }
        catch
        {
            // fall through to deterministic fallback
        }

        return
        [
            "Which services are part of the core business-critical user journey?",
            "What are the primary external systems and third-party APIs this platform depends on?",
            "Which data stores contain regulated or sensitive data, and what classifications apply?",
            "What trust boundaries exist between internet-facing services, internal services, and data layers?",
            "Which asynchronous integrations are critical for business continuity and must be reflected as explicit flows?",
            "Which environments are production-critical and should be visible in the default architecture view?",
            "What are the most important failure modes that threat modeling should prioritize first?"
        ];
    }
}
