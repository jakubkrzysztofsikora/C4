using C4.Modules.Identity.Application.InviteMember;
using C4.Modules.Identity.Domain.Member;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Identity.Api.Endpoints;

public sealed class InviteMemberEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId:guid}/members", async (Guid projectId, InviteMemberRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new InviteMemberCommand(projectId, request.ExternalUserId, request.Role), cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/projects/{projectId}/members/{result.Value.MemberId}", result.Value)
                : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }

    public sealed record InviteMemberRequest(string ExternalUserId, Role Role);
}
