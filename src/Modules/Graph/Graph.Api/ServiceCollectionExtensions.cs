using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Infrastructure.AI;
using C4.Modules.Graph.Infrastructure.Persistence;
using C4.Modules.Graph.Infrastructure.Persistence.Repositories;
using C4.Shared.Infrastructure.Behaviors;
using C4.Shared.Infrastructure.Endpoints;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace C4.Modules.Graph.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGraphModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            cfg.RegisterServicesFromAssembly(C4.Modules.Graph.Application.AssemblyReference.Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        var connectionString = configuration.GetConnectionString("Graph");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<GraphDbContext>(options => options.UseInMemoryDatabase("graph-dev"));
        }
        else
        {
            services.AddDbContext<GraphDbContext>(options => options.UseNpgsql(connectionString));
        }

        var ollamaEndpoint = configuration["Ollama:Endpoint"] ?? "http://localhost:11434";
        var chatModel = configuration["Ollama:ChatModel"] ?? "mistral-large-3:675b-cloud";

        var kernelBuilder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0070
        kernelBuilder.AddOllamaChatCompletion(chatModel, new Uri(ollamaEndpoint));
#pragma warning restore SKEXP0070

        var kernel = kernelBuilder.Build();
        services.AddSingleton(kernel);
        services.AddSingleton<IArchitectureAnalyzer>(sp =>
            new ArchitectureAnalysisPlugin(sp.GetRequiredService<Kernel>(), sp.GetService<C4.Shared.Kernel.Contracts.ILearningProvider>()));
        services.AddSingleton<IThreatDetector>(sp =>
            new ThreatDetectionPlugin(sp.GetRequiredService<Kernel>(), sp.GetService<C4.Shared.Kernel.Contracts.ILearningProvider>()));

        services.AddScoped<IArchitectureGraphRepository, ArchitectureGraphRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<GraphDbContext>());
        services.AddEndpoints(AssemblyReference.Assembly);
        return services;
    }
}
