using C4.Modules.Identity.Application.RegisterUser;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Identity.Api.Endpoints;

internal sealed class RegisterUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", async (
            RegisterUserRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new RegisterUserCommand(request.Email, request.Password, request.DisplayName),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/auth/register", result.Value)
                : Results.BadRequest(result.Error);
        })
        .WithTags("Auth");
    }

    public sealed record RegisterUserRequest(string Email, string Password, string DisplayName);
}
