using C4.Modules.Discovery.Application.McpServers;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Discovery.Api.Endpoints;

internal sealed class ListMcpServersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/mcp-servers", async (Guid projectId, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListMcpServersQuery(projectId), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithTags("McpServers")
        .RequireAuthorization();
    }
}
