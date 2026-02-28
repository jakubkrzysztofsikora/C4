using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.McpServers;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Discovery.Application.McpServers;

internal sealed class DeleteMcpServerHandler(
    IMcpServerConfigRepository repository,
    [FromKeyedServices("Discovery")] IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteMcpServerCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteMcpServerCommand request, CancellationToken cancellationToken)
    {
        await repository.DeleteAsync(new McpServerConfigId(request.Id), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
