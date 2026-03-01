using C4.Modules.Visualization.Application.Ports;
using C4.Modules.Visualization.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Visualization.Application.GetDiagram;

public sealed class GetDiagramHandler(
    IDiagramReadModel readModel,
    IProjectAuthorizationService authorizationService)
    : IRequestHandler<GetDiagramQuery, Result<GetDiagramResponse>>
{
    public async Task<Result<GetDiagramResponse>> Handle(GetDiagramQuery request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<GetDiagramResponse>.Failure(authCheck.Error);

        var json = await readModel.GetDiagramJsonAsync(request.ProjectId, cancellationToken);
        return json is null
            ? Result<GetDiagramResponse>.Failure(VisualizationErrors.DiagramNotFound(request.ProjectId))
            : Result<GetDiagramResponse>.Success(new GetDiagramResponse(request.ProjectId, json));
    }
}
