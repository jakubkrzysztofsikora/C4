using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.McpServers;

public sealed record ListMcpServersQuery(Guid ProjectId) : IRequest<Result<ListMcpServersResponse>>;

public sealed record ListMcpServersResponse(IReadOnlyCollection<McpServerItem> Servers);

public sealed record McpServerItem(Guid Id, string Name, string Endpoint, string AuthMode);
