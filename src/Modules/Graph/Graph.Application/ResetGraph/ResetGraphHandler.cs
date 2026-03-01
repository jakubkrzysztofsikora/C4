using C4.Modules.Graph.Application.Ports;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Graph.Application.ResetGraph;

public sealed class ResetGraphHandler(
    IArchitectureGraphRepository repository,
    [FromKeyedServices("Graph")] IUnitOfWork unitOfWork,
    IProjectAuthorizationService authorizationService
) : IRequestHandler<ResetGraphCommand, Result<ResetGraphResponse>>
{
    public async Task<Result<ResetGraphResponse>> Handle(ResetGraphCommand request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<ResetGraphResponse>.Failure(authCheck.Error);

        var graph = await repository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        if (graph is null)
        {
            return Result<ResetGraphResponse>.Success(new ResetGraphResponse(request.ProjectId));
        }

        await repository.DeleteAsync(graph, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ResetGraphResponse>.Success(new ResetGraphResponse(request.ProjectId));
    }
}
