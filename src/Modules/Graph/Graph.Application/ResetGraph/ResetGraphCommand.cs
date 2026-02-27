using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.ResetGraph;

public sealed record ResetGraphCommand(Guid ProjectId) : IRequest<Result<ResetGraphResponse>>;
