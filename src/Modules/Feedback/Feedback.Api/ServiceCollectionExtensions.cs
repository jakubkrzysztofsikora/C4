using C4.Modules.Feedback.Application.Ports;
using C4.Modules.Feedback.Infrastructure.AI;
using C4.Modules.Feedback.Infrastructure.Persistence;
using C4.Modules.Feedback.Infrastructure.Persistence.Repositories;
using C4.Shared.Infrastructure.AI;
using C4.Shared.Infrastructure.Behaviors;
using C4.Shared.Infrastructure.Endpoints;
using C4.Shared.Kernel;
using C4.Shared.Kernel.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddSharedSemanticKernel(configuration);

        services.AddKeyedSingleton<SemanticKernelCreationResult>("Feedback", (sp, _) =>
            sp.GetRequiredService<ISemanticKernelFactory>().Create("Feedback",
                [nameof(LearningAggregatorPlugin)]));
        services.AddSingleton<ILearningAggregator>(sp =>
        {
            var result = sp.GetRequiredKeyedService<SemanticKernelCreationResult>("Feedback");
            return new LearningAggregatorPlugin(result.Kernel);
        });

        services.AddScoped<IFeedbackEntryRepository, FeedbackEntryRepository>();
        services.AddScoped<ILearningInsightRepository, LearningInsightRepository>();
        services.AddScoped<ILearningProvider, C4.Modules.Feedback.Infrastructure.LearningProvider>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<FeedbackDbContext>());

        services.AddEndpoints(AssemblyReference.Assembly);
        return services;
    }
}
