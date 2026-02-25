using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Visualization.Application.GetViewPresets;

public sealed record GetViewPresetsQuery(Guid ProjectId) : IRequest<Result<GetViewPresetsResponse>>;
public sealed record GetViewPresetsResponse(IReadOnlyCollection<ViewPresetDto> Presets);
public sealed record ViewPresetDto(Guid Id, string Name, string Json);
