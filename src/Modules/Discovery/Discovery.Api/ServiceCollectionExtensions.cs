using C4.Modules.Discovery.Api.Adapters;
using C4.Modules.Discovery.Application.Adapters;
using C4.Modules.Discovery.Application.Ports;
using C4.Modules.Discovery.Infrastructure.AI;
using C4.Modules.Discovery.Infrastructure.Persistence;
using C4.Modules.Discovery.Infrastructure.Persistence.Repositories;
using C4.Shared.Infrastructure.Behaviors;
using C4.Shared.Infrastructure.Endpoints;
using C4.Shared.Kernel;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

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

        services.AddSingleton<IAzureSubscriptionRepository, InMemoryAzureSubscriptionRepository>();
        services.AddSingleton<IDiscoveredResourceRepository, InMemoryDiscoveredResourceRepository>();
        services.AddSingleton<IDriftResultRepository, InMemoryDriftResultRepository>();
        services.AddSingleton<IAzureResourceGraphClient, FakeAzureResourceGraphClient>();

        services.AddSingleton<BicepParser>();
        services.AddSingleton<TerraformParser>();
        services.AddSingleton<IIacStateParser, CompositeIacStateParser>();

        services.AddSingleton<IUnitOfWork, NoOpDiscoveryUnitOfWork>();

        var ollamaEndpoint = configuration["Ollama:Endpoint"] ?? "http://localhost:11434";
        var chatModel = configuration["Ollama:ChatModel"] ?? "mistral-large-3:675b-cloud";

        var kernelBuilder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0070
        kernelBuilder.AddOllamaChatCompletion(chatModel, new Uri(ollamaEndpoint));
#pragma warning restore SKEXP0070

        var kernel = kernelBuilder.Build();
        services.AddSingleton(kernel);
        services.AddSingleton<IResourceClassifier>(sp =>
            new ResourceClassifierPlugin(sp.GetRequiredService<Kernel>(), sp.GetService<C4.Shared.Kernel.Contracts.ILearningProvider>()));

        services.AddEndpoints(AssemblyReference.Assembly);

        return services;
    }
}
