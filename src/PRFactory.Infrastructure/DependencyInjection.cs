using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRFactory.Core.Application.Services;
using PRFactory.Domain.Interfaces;
using PRFactory.Infrastructure.Agents.Adapters;
using PRFactory.Infrastructure.Agents.Base;
using PRFactory.Infrastructure.Agents.Configuration;
using PRFactory.Infrastructure.Configuration;
using PRFactory.Infrastructure.Execution;
using PRFactory.Infrastructure.Git;
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

        // Register DbContext (only if not already registered, e.g., by tests)
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=prfactory.db";

        // Check if DbContext is already registered (e.g., by test setup with InMemory database)
        var dbContextDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ApplicationDbContext));
        if (dbContextDescriptor == null)
        {
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

                // Suppress EF Core service provider warning in test scenarios
                // This warning occurs when running 700+ tests that each create a new DbContext
                options.EnableServiceProviderCaching(false);
                options.ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning));
            });
        }

        // Register repositories
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IRepositoryRepository, RepositoryRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ITicketUpdateRepository, TicketUpdateRepository>();
        services.AddScoped<IWorkflowEventRepository, WorkflowEventRepository>();
        services.AddScoped<DomainCheckpointRepository, CheckpointRepository>();
        services.AddScoped<IAgentPromptTemplateRepository, AgentPromptTemplateRepository>();
        services.AddScoped<IErrorRepository, ErrorRepository>();
        services.AddScoped<ICodeReviewResultRepository, CodeReviewResultRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();

        // Team Review repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPlanReviewRepository, PlanReviewRepository>();
        services.AddScoped<IReviewCommentRepository, ReviewCommentRepository>();
        services.AddScoped<IInlineCommentAnchorRepository, InlineCommentAnchorRepository>();

        // Multi-LLM provider repositories
        services.AddScoped<ITenantLlmProviderRepository, TenantLlmProviderRepository>();

        // Register checkpoint store adapters
        services.AddScoped<WorkflowCheckpointStore, GraphCheckpointStoreAdapter>();
        services.AddScoped<Agents.Base.ICheckpointStore, BaseCheckpointStoreAdapter>();

        // Register workflow state store
        services.AddScoped<Agents.Graphs.IWorkflowStateStore, WorkflowStateStore>();

        // Register event publisher
        services.AddScoped<Agents.Graphs.IEventPublisher, Events.EventPublisher>();

        // Register caching
        services.AddMemoryCache();

        // Register configuration services
        services.AddScoped<PRFactory.Infrastructure.Configuration.ITenantConfigurationService, PRFactory.Infrastructure.Configuration.TenantConfigurationService>();
        services.AddScoped<PRFactory.Core.Application.Services.ITenantConfigurationService, Application.TenantConfigurationService>();

        // Register application services
        services.AddScoped<ITicketUpdateService, Application.TicketUpdateService>();
        services.AddScoped<ITicketApplicationService, Application.TicketApplicationService>();
        services.AddScoped<IRepositoryApplicationService, Application.RepositoryApplicationService>();
        services.AddScoped<IRepositoryService, Application.RepositoryService>();
        services.AddScoped<ITenantApplicationService, Application.TenantApplicationService>();
        services.AddScoped<IErrorApplicationService, Application.ErrorApplicationService>();
        services.AddScoped<ITenantContext, Application.TenantContext>();
        services.AddScoped<IQuestionApplicationService, Application.QuestionApplicationService>();
        services.AddScoped<IWorkflowEventApplicationService, Application.WorkflowEventApplicationService>();
        services.AddScoped<IPlanService, Application.PlanService>();

        // Team Review application services
        services.AddScoped<IUserService, Application.UserService>();
        services.AddScoped<IPlanReviewService, Application.PlanReviewService>();
        services.AddScoped<ICurrentUserService, Application.CurrentUserService>();
        services.AddScoped<IProvisioningService, Application.ProvisioningService>();
        services.AddScoped<IUserManagementService, Application.UserManagementService>();
        services.AddScoped<IChecklistTemplateService, Application.ChecklistTemplateService>();

        // Multi-LLM provider services
        services.AddScoped<ITenantLlmProviderService, Application.TenantLlmProviderService>();

        // Register IHttpContextAccessor for CurrentUserService
        services.AddHttpContextAccessor();

        // Register agent prompt services
        services.AddScoped<Agents.Services.IAgentPromptService, Agents.Services.AgentPromptService>();
        services.AddScoped<Agents.Services.AgentPromptLoaderService>();

        // Register architecture context service for enhanced planning prompts
        services.AddScoped<IArchitectureContextService, Agents.Services.ArchitectureContextService>();

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

        // Register planning agents (Epic 03 - Deep Planning)
        services.AddTransient<Agents.Planning.PmUserStoriesAgent>();
        services.AddTransient<Agents.Planning.ArchitectApiDesignAgent>();
        services.AddTransient<Agents.Planning.ArchitectDbSchemaAgent>();
        services.AddTransient<Agents.Planning.QaTestCasesAgent>();
        services.AddTransient<Agents.Planning.TechLeadImplementationAgent>();
        services.AddTransient<Agents.Planning.PlanArtifactStorageAgent>();
        services.AddTransient<Agents.Planning.FeedbackAnalysisAgent>();
        services.AddTransient<Agents.Planning.PlanRevisionAgent>();

        // Register agent executor
        services.AddScoped<Agents.Graphs.IAgentExecutor, Agents.Graphs.AgentExecutor>();

        // Register CLI agent abstraction layer
        services.AddScoped<IProcessExecutor, ProcessExecutor>();

        // Configure ClaudeCodeCliOptions
        services.Configure<ClaudeCodeCliOptions>(
            configuration.GetSection("ClaudeCodeCli"));

        services.AddScoped<ClaudeCodeCliAdapter>();
        services.AddScoped<CodexCliAdapter>();

        // Register default CLI agent (Claude Code)
        services.AddScoped<ICliAgent>(sp => sp.GetRequiredService<ClaudeCodeCliAdapter>());

        // Register LLM providers (multi-provider support)
        services.Configure<PRFactory.Core.Configuration.LlmProvidersOptions>(
            configuration.GetSection("LlmProviders"));

        services.AddScoped<PRFactory.Infrastructure.Agents.Adapters.ClaudeCodeCliLlmProvider>();
        services.AddScoped<PRFactory.Infrastructure.Agents.Adapters.GeminiCliAdapter>();
        services.AddScoped<PRFactory.Infrastructure.Agents.Adapters.OpenAiCliAdapter>();

        // Register LLM provider factory and prompt loader
        services.AddScoped<PRFactory.Core.Application.LLM.ILlmProviderFactory, PRFactory.Infrastructure.Application.LlmProviderFactory>();
        services.AddScoped<PRFactory.Core.Application.LLM.IPromptLoaderService, PRFactory.Infrastructure.Application.PromptLoaderService>();

        // Register Git platform integration (LocalGitService, providers, and GitPlatformService)
        services.AddGitPlatformIntegration(sp =>
        {
            var repoRepository = sp.GetRequiredService<IRepositoryRepository>();

            // Create repository getter function
            return async (Guid repositoryId, CancellationToken ct) =>
            {
                var repo = await repoRepository.GetByIdAsync(repositoryId, ct);
                if (repo == null)
                    throw new InvalidOperationException($"Repository {repositoryId} not found");

                return new Git.Providers.RepositoryEntity
                {
                    Id = repo.Id,
                    Name = repo.Name,
                    CloneUrl = repo.CloneUrl,
                    AccessToken = repo.AccessToken,
                    DefaultBranch = repo.DefaultBranch,
                    GitPlatform = repo.GitPlatform
                };
            };
        });

        // Register agent framework (middleware, agent registry)
        services.AddAgentFramework(configuration);

        // Register workflow graphs
        services.AddScoped<Agents.Graphs.RefinementGraph>();
        services.AddScoped<Agents.Graphs.PlanningGraph>();
        services.AddScoped<Agents.Graphs.ImplementationGraph>();
        services.AddScoped<Agents.Graphs.CodeReviewGraph>();

        // Register specialized agents for code review workflow
        services.AddTransient<Agents.Specialized.CodeReviewAgent>();
        services.AddTransient<Agents.Specialized.PostReviewCommentsAgent>();
        services.AddTransient<Agents.Specialized.PostApprovalCommentAgent>();

        // Register workflow orchestrator
        services.AddScoped<Agents.Graphs.IWorkflowOrchestrator, Agents.Graphs.WorkflowOrchestrator>();

        // Register event broadcaster (stub for infrastructure layer)
        // Note: The real implementation (SignalREventBroadcaster) should be registered in the Web layer
        services.AddScoped<Events.IEventBroadcaster>(sp =>
        {
            // Return a stub implementation if Web layer hasn't registered one
            // This allows Infrastructure tests to run without the Web layer
            return new Events.StubEventBroadcaster();
        });

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
