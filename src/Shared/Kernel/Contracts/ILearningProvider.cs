namespace C4.Shared.Kernel.Contracts;

public interface ILearningProvider
{
    Task<IReadOnlyCollection<LearningDto>> GetActiveLearningsAsync(Guid projectId, string category, CancellationToken cancellationToken);
}

public sealed record LearningDto(string Description, double Confidence, string InsightType);
