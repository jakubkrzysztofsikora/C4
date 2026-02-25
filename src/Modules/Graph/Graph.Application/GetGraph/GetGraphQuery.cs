using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetGraph;

public sealed record GetGraphQuery(Guid ProjectId, string? Level) : IRequest<Result<GraphDto>>;
