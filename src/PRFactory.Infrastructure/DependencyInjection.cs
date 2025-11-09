using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.Adapters;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Configuration;
using PRFactory.Infrastructure.Execution;
using PRFactory.Infrastructure.Persistence;
using PRFactory.Infrastructure.Persistence.Encryption;
using PRFactory.Infrastructure.Persistence.Repositories;
using DomainCheckpointRepository = PRFactory.Domain.Interfaces.ICheckpointRepository;
using WorkflowCheckpointStore = PRFactory.Infrastructure.Agents.ICheckpointStore;

namespace PRFactory.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure services with dependency injection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure layer services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register encryption service
        var encryptionKey = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption key not configured. Set 'Encryption:Key' in configuration.");

        services.AddSingleton<IEncryptionService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<AesEncryptionService>>();
            return new AesEncryptionService(encryptionKey, logger);
        });

        // Register DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=prfactory.db";

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlite(connectionString);

            // Enable sensitive data logging in development
            if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }

            // Enable detailed errors in development
            if (configuration.GetValue<bool>("Logging:EnableDetailedErrors"))
            {
                options.EnableDetailedErrors();
            }
        });

        // Register repositories
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IRepositoryRepository, RepositoryRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ITicketUpdateRepository, TicketUpdateRepository>();
        services.AddScoped<IWorkflowEventRepository, WorkflowEventRepository>();
        services.AddScoped<DomainCheckpointRepository, CheckpointRepository>();
        services.AddScoped<IAgentPromptTemplateRepository, AgentPromptTemplateRepository>();
        services.AddScoped<IErrorRepository, ErrorRepository>();

        // Team Review repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPlanReviewRepository, PlanReviewRepository>();
        services.AddScoped<IReviewCommentRepository, ReviewCommentRepository>();

        // Register checkpoint store adapter
        services.AddScoped<WorkflowCheckpointStore, GraphCheckpointStoreAdapter>();

        // Register workflow state store
        services.AddScoped<Agents.Graphs.IWorkflowStateStore, WorkflowStateStore>();

        // Register event publisher
        services.AddScoped<Agents.Graphs.IEventPublisher, Events.EventPublisher>();

        // Register caching
        services.AddMemoryCache();

        // Register configuration services
        services.AddScoped<ITenantConfigurationService, TenantConfigurationService>();

        // Register application services
        services.AddScoped<ITicketUpdateService, Application.TicketUpdateService>();
        services.AddScoped<ITicketApplicationService, Application.TicketApplicationService>();
        services.AddScoped<IRepositoryApplicationService, Application.RepositoryApplicationService>();
        services.AddScoped<ITenantApplicationService, Application.TenantApplicationService>();
        services.AddScoped<IErrorApplicationService, Application.ErrorApplicationService>();
        services.AddScoped<ITenantContext, Application.TenantContext>();
        services.AddScoped<IQuestionApplicationService, Application.QuestionApplicationService>();
        services.AddScoped<IWorkflowEventApplicationService, Application.WorkflowEventApplicationService>();
        services.AddScoped<IPlanService, Application.PlanService>();

        // Team Review application services
        services.AddScoped<IUserService, Application.UserService>();
        services.AddScoped<IPlanReviewService, Application.PlanReviewService>();
        services.AddScoped<ICurrentUserService, Application.StubCurrentUserService>();

        // Register agent prompt services
        services.AddScoped<Agents.Services.IAgentPromptService, Agents.Services.AgentPromptService>();
        services.AddScoped<Agents.Services.AgentPromptLoaderService>();

        // Register context builder for AI agents
        services.AddScoped<Claude.IContextBuilder, Claude.ContextBuilder>();

        // Register agents
        services.AddTransient<Agents.TriggerAgent>();
        services.AddTransient<Agents.RepositoryCloneAgent>();
        services.AddTransient<Agents.AnalysisAgent>();
        services.AddTransient<Agents.QuestionGenerationAgent>();
        services.AddTransient<Agents.JiraPostAgent>();
        services.AddTransient<Agents.HumanWaitAgent>();
        services.AddTransient<Agents.AnswerProcessingAgent>();
        services.AddTransient<Agents.PlanningAgent>();
        services.AddTransient<Agents.GitPlanAgent>();
        services.AddTransient<Agents.ImplementationAgent>();
        services.AddTransient<Agents.GitCommitAgent>();
        services.AddTransient<Agents.PullRequestAgent>();
        services.AddTransient<Agents.CompletionAgent>();
        services.AddTransient<Agents.ApprovalCheckAgent>();
        services.AddTransient<Agents.ErrorHandlingAgent>();

        // Register agent executor
        services.AddScoped<Agents.Graphs.IAgentExecutor, Agents.Graphs.AgentExecutor>();

        // Register CLI agent abstraction layer
        services.AddScoped<IProcessExecutor, ProcessExecutor>();

        // Configure ClaudeDesktopCliOptions
        services.Configure<ClaudeDesktopCliOptions>(
            configuration.GetSection("ClaudeDesktopCli"));

        services.AddScoped<ClaudeDesktopCliAdapter>();
        services.AddScoped<CodexCliAdapter>();

        // Register default CLI agent (Claude Desktop)
        services.AddScoped<ICliAgent>(sp => sp.GetRequiredService<ClaudeDesktopCliAdapter>());

        // Register database seeder
        services.AddScoped<DbSeeder>();

        return services;
    }

    /// <summary>
    /// Generates a new encryption key for use in configuration.
    /// </summary>
    /// <returns>A base64-encoded 256-bit encryption key</returns>
    public static string GenerateEncryptionKey()
    {
        return EncryptionKeyGenerator.GenerateKey();
    }
}
