using C4.Modules.Identity.Application.UpdateMemberRole;
using C4.Modules.Identity.Domain.Member;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Identity.Api.Endpoints;

public sealed class UpdateMemberRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/projects/{projectId:guid}/members/{memberId:guid}/role", async (Guid projectId, Guid memberId, UpdateMemberRoleRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new UpdateMemberRoleCommand(projectId, memberId, request.Role), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }

    public sealed record UpdateMemberRoleRequest(Role Role);
}
