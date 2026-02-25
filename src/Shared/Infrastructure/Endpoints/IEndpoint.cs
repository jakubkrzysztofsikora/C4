using Microsoft.AspNetCore.Routing;

namespace C4.Shared.Infrastructure.Endpoints;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
