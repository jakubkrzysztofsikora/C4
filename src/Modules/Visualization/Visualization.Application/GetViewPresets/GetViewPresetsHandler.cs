using C4.Modules.Visualization.Application.Ports;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Visualization.Application.GetViewPresets;

public sealed class GetViewPresetsHandler(IViewPresetRepository repository)
    : IRequestHandler<GetViewPresetsQuery, Result<GetViewPresetsResponse>>
{
    public async Task<Result<GetViewPresetsResponse>> Handle(GetViewPresetsQuery request, CancellationToken cancellationToken)
    {
        var presets = await repository.GetByProjectAsync(request.ProjectId, cancellationToken);
        return Result<GetViewPresetsResponse>.Success(new GetViewPresetsResponse(
            presets.Select(p => new ViewPresetDto(p.Id, p.Name, p.Json)).ToArray()));
    }
}
