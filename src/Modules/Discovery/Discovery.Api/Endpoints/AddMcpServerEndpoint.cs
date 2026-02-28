using C4.Modules.Discovery.Application.McpServers;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Discovery.Api.Endpoints;

internal sealed class AddMcpServerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId:guid}/mcp-servers", async (Guid projectId, AddMcpServerRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new AddMcpServerCommand(projectId, request.Name, request.Endpoint, request.AuthMode), cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/projects/{projectId}/mcp-servers/{result.Value.Id}", result.Value)
                : Results.BadRequest(result.Error);
        })
        .WithTags("McpServers")
        .RequireAuthorization();
    }

    public sealed record AddMcpServerRequest(string Name, string Endpoint, string AuthMode = "None");
}
