using C4.Modules.Discovery.Application.ConfigureIacRepository;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Discovery.Api.Endpoints;

public sealed class ConfigureIacRepositoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/discovery/subscriptions/{subscriptionId:guid}/iac-config", async (
            Guid subscriptionId,
            ConfigureIacRepositoryRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ConfigureIacRepositoryCommand(
                subscriptionId,
                request.GitRepoUrl,
                request.GitPatToken,
                request.GitBranch,
                request.GitRootPath), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }

    public sealed record ConfigureIacRepositoryRequest(
        string? GitRepoUrl,
        string? GitPatToken,
        string? GitBranch,
        string? GitRootPath);
}
