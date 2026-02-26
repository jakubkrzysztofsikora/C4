using C4.Modules.Feedback.Application.Ports;
using C4.Modules.Feedback.Infrastructure.AI;
using C4.Modules.Feedback.Infrastructure.Persistence;
using C4.Modules.Feedback.Infrastructure.Persistence.Repositories;
using C4.Shared.Infrastructure.Behaviors;
using C4.Shared.Infrastructure.Endpoints;
using C4.Shared.Kernel;
using C4.Shared.Kernel.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace C4.Modules.Feedback.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFeedbackModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            cfg.RegisterServicesFromAssembly(C4.Modules.Feedback.Application.AssemblyReference.Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        var connectionString = configuration.GetConnectionString("Feedback");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<FeedbackDbContext>(options => options.UseInMemoryDatabase("feedback-dev"));
        }
        else
        {
            services.AddDbContext<FeedbackDbContext>(options => options.UseNpgsql(connectionString));
        }

        var ollamaEndpoint = configuration["Ollama:Endpoint"] ?? "http://localhost:11434";
        var chatModel = configuration["Ollama:ChatModel"] ?? "mistral-large-3:675b-cloud";

        var kernelBuilder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0070
        kernelBuilder.AddOllamaChatCompletion(chatModel, new Uri(ollamaEndpoint));
#pragma warning restore SKEXP0070

        var feedbackKernel = kernelBuilder.Build();
        services.AddKeyedSingleton("feedback", feedbackKernel);
        services.AddSingleton<ILearningAggregator>(sp =>
            new LearningAggregatorPlugin(sp.GetRequiredKeyedService<Kernel>("feedback")));

        services.AddScoped<IFeedbackEntryRepository, FeedbackEntryRepository>();
        services.AddScoped<ILearningInsightRepository, LearningInsightRepository>();
        services.AddScoped<ILearningProvider, C4.Modules.Feedback.Infrastructure.LearningProvider>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<FeedbackDbContext>());

        services.AddEndpoints(AssemblyReference.Assembly);
        return services;
    }
}
