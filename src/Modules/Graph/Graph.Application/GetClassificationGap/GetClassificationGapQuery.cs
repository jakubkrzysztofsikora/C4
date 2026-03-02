using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetClassificationGap;

public sealed record GetClassificationGapQuery(Guid ProjectId) : IRequest<Result<ClassificationGapResponse>>;

public sealed record ClassificationGapResponse(
    int TotalNodes,
    int MismatchCount,
    IReadOnlyCollection<MetricCount> CurrentLevelCounts,
    IReadOnlyCollection<MetricCount> EffectiveLevelCounts,
    IReadOnlyCollection<MetricCount> CurrentServiceTypeCounts,
    IReadOnlyCollection<MetricCount> EffectiveServiceTypeCounts,
    IReadOnlyCollection<TypeMismatchCount> TopMismatchedTypes,
    IReadOnlyCollection<MismatchSample> Samples);

public sealed record MetricCount(string Key, int Count);
public sealed record TypeMismatchCount(string ArmType, int Count);
public sealed record MismatchSample(string NodeName, string ExternalResourceId, string ArmType, string CurrentLevel, string EffectiveLevel, string CurrentServiceType, string EffectiveServiceType);
