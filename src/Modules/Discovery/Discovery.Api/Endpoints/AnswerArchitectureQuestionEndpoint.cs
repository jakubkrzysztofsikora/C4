using C4.Modules.Discovery.Application.ArchitectureContext;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Discovery.Api.Endpoints;

public sealed class AnswerArchitectureQuestionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/projects/{projectId:guid}/architecture-context/questions/{questionId:guid}/answer", async (Guid projectId, Guid questionId, AnswerQuestionRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AnswerArchitectureQuestionCommand(projectId, questionId, request.Answer), ct);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }

    public sealed record AnswerQuestionRequest(string Answer);
}
