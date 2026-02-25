using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Visualization.Application.SaveViewPreset;

public sealed record SaveViewPresetCommand(Guid ProjectId, string Name, string Json) : IRequest<Result<SaveViewPresetResponse>>;
public sealed record SaveViewPresetResponse(Guid PresetId, Guid ProjectId, string Name);
