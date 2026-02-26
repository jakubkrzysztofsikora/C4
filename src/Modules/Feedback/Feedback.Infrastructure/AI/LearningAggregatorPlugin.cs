using System.Text;
using C4.Modules.Feedback.Application.Ports;
using C4.Modules.Feedback.Domain.FeedbackEntry;
using C4.Modules.Feedback.Domain.Learning;
using Microsoft.SemanticKernel;

namespace C4.Modules.Feedback.Infrastructure.AI;

public sealed class LearningAggregatorPlugin(Kernel kernel) : ILearningAggregator
{
    public async Task<IReadOnlyCollection<LearningInsight>> AggregateAsync(
        Guid projectId,
        IReadOnlyCollection<FeedbackEntry> entries,
        CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
        {
            return [];
        }

        var feedbackDescription = BuildFeedbackDescription(entries);

        var prompt = $$"""
            Analyze the following user feedback entries on AI-generated architecture diagrams and classifications.
            Identify recurring patterns, common corrections, and actionable insights.

            Feedback entries:
            {{feedbackDescription}}

            For each pattern you identify, respond with exactly one line in this format:
            INSIGHT|<InsightType>|<Description>|<Confidence>|<FeedbackCount>

            InsightType must be one of: ClassificationPattern, NamingConvention, RelationshipRule, LayoutPreference, ThreatPattern, ResourceTypeMapping
            Confidence must be a decimal between 0.0 and 1.0
            FeedbackCount must be the number of feedback entries that support this insight

            Identify up to 5 most significant patterns. Only include patterns with confidence >= 0.5.
            """;

        var result = await kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        var text = result.GetValue<string>() ?? string.Empty;

        var primaryCategory = entries
            .GroupBy(e => e.Category)
            .OrderByDescending(g => g.Count())
            .First()
            .Key;

        return ParseInsights(text, projectId, primaryCategory);
    }

    private static string BuildFeedbackDescription(IReadOnlyCollection<FeedbackEntry> entries)
    {
        var sb = new StringBuilder();
        foreach (var entry in entries.Take(50))
        {
            sb.AppendLine($"- Category: {entry.Category}, Rating: {entry.Rating.Score}/5, Target: {entry.Target.TargetType}");
            if (!string.IsNullOrWhiteSpace(entry.Comment))
                sb.AppendLine($"  Comment: {entry.Comment}");
            if (entry.NodeCorrection is not null)
                sb.AppendLine($"  NodeCorrection: {entry.NodeCorrection.OriginalName} -> {entry.NodeCorrection.CorrectedName}, Level: {entry.NodeCorrection.OriginalLevel} -> {entry.NodeCorrection.CorrectedLevel}");
            if (entry.ClassificationCorrection is not null)
                sb.AppendLine($"  ClassificationCorrection: {entry.ClassificationCorrection.ArmResourceType}, ServiceType: {entry.ClassificationCorrection.OriginalServiceType} -> {entry.ClassificationCorrection.CorrectedServiceType}");
            if (entry.EdgeCorrection is not null)
                sb.AppendLine($"  EdgeCorrection: {entry.EdgeCorrection.OriginalRelationship} -> {entry.EdgeCorrection.CorrectedRelationship}, ShouldExist: {entry.EdgeCorrection.ShouldExist}");
        }
        return sb.ToString();
    }

    private static List<LearningInsight> ParseInsights(string text, Guid projectId, FeedbackCategory category)
    {
        var insights = new List<LearningInsight>();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("INSIGHT|", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = trimmed.Split('|');
            if (parts.Length < 5)
                continue;

            if (!Enum.TryParse<InsightType>(parts[1].Trim(), true, out var insightType))
                continue;

            var description = parts[2].Trim();
            if (!double.TryParse(parts[3].Trim(), out var confidence) || confidence < 0.5)
                continue;

            if (!int.TryParse(parts[4].Trim(), out var feedbackCount))
                feedbackCount = 1;

            insights.Add(LearningInsight.Aggregate(projectId, category, insightType, description, confidence, feedbackCount));
        }

        return insights;
    }
}
