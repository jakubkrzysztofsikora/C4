using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.McpServers;

public sealed record AddMcpServerCommand(Guid ProjectId, string Name, string Endpoint, string AuthMode = "None")
    : IRequest<Result<McpServerItem>>;
