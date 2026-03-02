using C4.Modules.Discovery.Application.ArchitectureContext;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Discovery.Api.Endpoints;

public sealed class GenerateArchitectureQuestionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId:guid}/architecture-context/questions:generate", async (Guid projectId, GenerateQuestionsRequest? request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GenerateArchitectureQuestionsCommand(projectId, request?.CurrentResourceCount, request?.Force ?? true),
                ct);

            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }

    public sealed record GenerateQuestionsRequest(int? CurrentResourceCount, bool Force = true);
}
