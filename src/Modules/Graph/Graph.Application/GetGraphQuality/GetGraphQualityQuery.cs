using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetGraphQuality;

public sealed record GetGraphQualityQuery(Guid ProjectId) : IRequest<Result<GetGraphQualityResponse>>;

public sealed record GetGraphQualityResponse(
    Guid ProjectId,
    int TotalNodes,
    int FallbackClassificationCount,
    int UnknownEnvironmentCount,
    int NonRuntimeNodeCount,
    int RawDeclarationLabelCount,
    DateTime GeneratedAtUtc);
