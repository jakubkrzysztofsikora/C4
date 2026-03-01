using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.McpServers;

internal sealed class ListMcpServersHandler(
    IMcpServerConfigRepository repository,
    IProjectAuthorizationService authorizationService)
    : IRequestHandler<ListMcpServersQuery, Result<ListMcpServersResponse>>
{
    public async Task<Result<ListMcpServersResponse>> Handle(ListMcpServersQuery request, CancellationToken cancellationToken)
    {
        Result<bool> authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (authCheck.IsFailure)
        {
            return Result<ListMcpServersResponse>.Failure(authCheck.Error);
        }

        IReadOnlyCollection<Domain.McpServers.McpServerConfig> configs = await repository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        McpServerItem[] items = configs.Select(c => new McpServerItem(c.Id.Value, c.Name, c.Endpoint, c.AuthMode)).ToArray();
        return Result<ListMcpServersResponse>.Success(new ListMcpServersResponse(items));
    }
}
