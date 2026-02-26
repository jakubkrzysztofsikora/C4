using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Infrastructure.Persistence;
using C4.Modules.Identity.Infrastructure.Repositories;
using C4.Modules.Identity.Infrastructure.Security;
using C4.Shared.Infrastructure.Behaviors;
using C4.Shared.Infrastructure.Endpoints;
using C4.Shared.Kernel;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Identity.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            cfg.RegisterServicesFromAssembly(C4.Modules.Identity.Application.AssemblyReference.Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        services.AddValidatorsFromAssembly(C4.Modules.Identity.Application.AssemblyReference.Assembly);

        var connectionString = configuration.GetConnectionString("Identity");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<IdentityDbContext>(options => options.UseInMemoryDatabase("identity-dev"));
        }
        else
        {
            services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(connectionString));
        }

        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddEndpoints(AssemblyReference.Assembly);
        return services;
    }
}
