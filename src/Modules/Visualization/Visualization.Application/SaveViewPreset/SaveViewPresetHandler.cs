using C4.Modules.Visualization.Application.Ports;
using C4.Modules.Visualization.Domain.Preset;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Visualization.Application.SaveViewPreset;

public sealed class SaveViewPresetHandler(
    IViewPresetRepository repository,
    [FromKeyedServices("Visualization")] IUnitOfWork unitOfWork,
    IProjectAuthorizationService authorizationService)
    : IRequestHandler<SaveViewPresetCommand, Result<SaveViewPresetResponse>>
{
    public async Task<Result<SaveViewPresetResponse>> Handle(SaveViewPresetCommand request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<SaveViewPresetResponse>.Failure(authCheck.Error);

        var preset = ViewPreset.Create(request.ProjectId, request.Name, request.Json);
        await repository.AddAsync(preset, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<SaveViewPresetResponse>.Success(new SaveViewPresetResponse(preset.Id, preset.ProjectId, preset.Name));
    }
}
