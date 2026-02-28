using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Discovery.Application.McpServers;

public sealed record DeleteMcpServerCommand(Guid Id) : IRequest<Result<bool>>;
