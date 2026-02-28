using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Domain.McpServers;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Discovery.Application.McpServers;

internal sealed class AddMcpServerHandler(
    IMcpServerConfigRepository repository,
    [FromKeyedServices("Discovery")] IUnitOfWork unitOfWork)
    : IRequestHandler<AddMcpServerCommand, Result<McpServerItem>>
{
    public async Task<Result<McpServerItem>> Handle(AddMcpServerCommand request, CancellationToken cancellationToken)
    {
        McpServerConfig config = McpServerConfig.Create(request.ProjectId, request.Name, request.Endpoint, request.AuthMode);
        await repository.AddAsync(config, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<McpServerItem>.Success(new McpServerItem(config.Id.Value, config.Name, config.Endpoint, config.AuthMode));
    }
}
