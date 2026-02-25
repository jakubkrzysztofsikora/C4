using C4.Modules.Discovery.Api.Adapters;
using C4.Modules.Discovery.Application.Adapters;
using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Infrastructure.Persistence;
using C4.Modules.Discovery.Infrastructure.Persistence.Repositories;
using C4.Shared.Infrastructure.Behaviors;
using C4.Shared.Infrastructure.Endpoints;
using C4.Shared.Kernel;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Discovery.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiscoveryModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            cfg.RegisterServicesFromAssembly(C4.Modules.Discovery.Application.AssemblyReference.Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        services.AddValidatorsFromAssembly(C4.Modules.Discovery.Application.AssemblyReference.Assembly);

        var connectionString = configuration.GetConnectionString("Discovery");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<DiscoveryDbContext>(options => options.UseInMemoryDatabase("discovery-dev"));
        }
        else
        {
            services.AddDbContext<DiscoveryDbContext>(options => options.UseNpgsql(connectionString));
        }

        services.AddScoped<IAzureSubscriptionRepository, AzureSubscriptionRepository>();
        services.AddScoped<IDiscoveredResourceRepository, DiscoveredResourceRepository>();
        services.AddScoped<IDriftResultRepository, DriftResultRepository>();
        services.AddSingleton<IAzureResourceGraphClient, FakeAzureResourceGraphClient>();

        services.AddSingleton<BicepParser>();
        services.AddSingleton<TerraformParser>();
        services.AddSingleton<IIacStateParser, CompositeIacStateParser>();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DiscoveryDbContext>());
        services.AddEndpoints(AssemblyReference.Assembly);

        return services;
    }
}
