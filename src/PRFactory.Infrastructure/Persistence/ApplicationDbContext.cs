using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Persistence.Encryption;
using PRFactory.Infrastructure.Persistence.Entities;
using System.Text.Json;
using TenantConfig = PRFactory.Infrastructure.Persistence.Configurations.TenantConfiguration;
using RepositoryConfig = PRFactory.Infrastructure.Persistence.Configurations.RepositoryConfiguration;
using TicketConfig = PRFactory.Infrastructure.Persistence.Configurations.TicketConfiguration;
using TicketUpdateConfig = PRFactory.Infrastructure.Persistence.Configurations.TicketUpdateConfiguration;
using WorkflowEventConfig = PRFactory.Infrastructure.Persistence.Configurations.WorkflowEventConfiguration;
using WorkflowStateConfig = PRFactory.Infrastructure.Persistence.Configurations.WorkflowStateConfiguration;
using CheckpointConfig = PRFactory.Infrastructure.Persistence.Configurations.CheckpointConfiguration;
using AgentPromptTemplateConfig = PRFactory.Infrastructure.Persistence.Configurations.AgentPromptTemplateConfiguration;
using UserConfig = PRFactory.Infrastructure.Persistence.Configurations.UserConfiguration;
using PlanReviewConfig = PRFactory.Infrastructure.Persistence.Configurations.PlanReviewConfiguration;
using ReviewCommentConfig = PRFactory.Infrastructure.Persistence.Configurations.ReviewCommentConfiguration;
using TenantLlmProviderConfig = PRFactory.Infrastructure.Persistence.Configurations.TenantLlmProviderConfiguration;
using CodeReviewResultConfig = PRFactory.Infrastructure.Persistence.Configurations.CodeReviewResultConfiguration;
using AgentConfigurationConfig = PRFactory.Infrastructure.Persistence.Configurations.AgentConfigurationConfiguration;
using AgentExecutionLogConfig = PRFactory.Infrastructure.Persistence.Configurations.AgentExecutionLogConfiguration;
using InlineCommentAnchorConfig = PRFactory.Infrastructure.Persistence.Configurations.InlineCommentAnchorConfiguration;
using ReviewChecklistConfig = PRFactory.Infrastructure.Persistence.Configurations.ReviewChecklistConfiguration;
using ChecklistItemConfig = PRFactory.Infrastructure.Persistence.Configurations.ChecklistItemConfiguration;

namespace PRFactory.Infrastructure.Persistence;

/// <summary>
/// Main Entity Framework Core DbContext for PRFactory.
/// Handles all database operations for Tenants, Repositories, and Tickets.
/// Includes ASP.NET Core Identity tables for authentication.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<ApplicationDbContext> _logger;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IEncryptionService encryptionService,
        ILogger<ApplicationDbContext> logger)
        : base(options)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // DbSets
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Repository> Repositories => Set<Repository>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketUpdate> TicketUpdates => Set<TicketUpdate>();
    public DbSet<WorkflowEvent> WorkflowEvents => Set<WorkflowEvent>();
    public DbSet<WorkflowStateEntity> WorkflowStates => Set<WorkflowStateEntity>();
    public DbSet<Checkpoint> Checkpoints => Set<Checkpoint>();
    public DbSet<AgentPromptTemplate> AgentPromptTemplates => Set<AgentPromptTemplate>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

    // Team Review DbSets
    // Note: This hides IdentityDbContext's Users DbSet<IdentityUser> property
    public new DbSet<User> Users => Set<User>();
    public DbSet<PlanReview> PlanReviews => Set<PlanReview>();
    public DbSet<ReviewComment> ReviewComments => Set<ReviewComment>();
    public DbSet<InlineCommentAnchor> InlineCommentAnchors => Set<InlineCommentAnchor>();
    public DbSet<ReviewChecklist> ReviewChecklists => Set<ReviewChecklist>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();

    // LLM Provider DbSets
    public DbSet<TenantLlmProvider> TenantLlmProviders => Set<TenantLlmProvider>();

    // Code Review DbSets
    public DbSet<CodeReviewResult> CodeReviewResults => Set<CodeReviewResult>();

    // Agent Framework DbSets
    public DbSet<AgentConfiguration> AgentConfigurations => Set<AgentConfiguration>();
    public DbSet<AgentExecutionLog> AgentExecutionLogs => Set<AgentExecutionLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // IMPORTANT: Call base first to configure Identity tables
        base.OnModelCreating(builder);

        // Apply entity configurations
        builder.ApplyConfiguration(new TenantConfig(_encryptionService));
        builder.ApplyConfiguration(new RepositoryConfig(_encryptionService));
        builder.ApplyConfiguration(new TicketConfig());
        builder.ApplyConfiguration(new TicketUpdateConfig());
        builder.ApplyConfiguration(new WorkflowEventConfig());
        builder.ApplyConfiguration(new WorkflowStateConfig());
        builder.ApplyConfiguration(new CheckpointConfig());
        builder.ApplyConfiguration(new AgentPromptTemplateConfig());

        // Team Review configurations
        builder.ApplyConfiguration(new UserConfig());
        builder.ApplyConfiguration(new PlanReviewConfig());
        builder.ApplyConfiguration(new ReviewCommentConfig());
        builder.ApplyConfiguration(new InlineCommentAnchorConfig());
        builder.ApplyConfiguration(new ReviewChecklistConfig());
        builder.ApplyConfiguration(new ChecklistItemConfig());

        // LLM Provider configuration
        builder.ApplyConfiguration(new TenantLlmProviderConfig(_encryptionService));

        // Code Review configuration
        builder.ApplyConfiguration(new CodeReviewResultConfig());

        // Agent Framework configurations
        builder.ApplyConfiguration(new AgentConfigurationConfig());
        builder.ApplyConfiguration(new AgentExecutionLogConfig());

        // Add indexes for common queries
        AddIndexes(builder);

        // Configure enum conversions
        ConfigureEnumConversions(builder);
    }

    private void AddIndexes(ModelBuilder builder)
    {
        // Ticket indexes
        builder.Entity<Ticket>()
            .HasIndex(t => t.TicketKey)
            .IsUnique();

        builder.Entity<Ticket>()
            .HasIndex(t => t.TenantId);

        builder.Entity<Ticket>()
            .HasIndex(t => t.RepositoryId);

        builder.Entity<Ticket>()
            .HasIndex(t => t.State);

        builder.Entity<Ticket>()
            .HasIndex(t => new { t.State, t.TenantId });

        builder.Entity<Ticket>()
            .HasIndex(t => t.CreatedAt);

        // Repository indexes
        builder.Entity<Repository>()
            .HasIndex(r => r.TenantId);

        builder.Entity<Repository>()
            .HasIndex(r => r.CloneUrl)
            .IsUnique();

        builder.Entity<Repository>()
            .HasIndex(r => r.GitPlatform);

        // Tenant indexes
        builder.Entity<Tenant>()
            .HasIndex(t => t.Name)
            .IsUnique();

        builder.Entity<Tenant>()
            .HasIndex(t => t.IsActive);

        // WorkflowEvent indexes
        builder.Entity<WorkflowEvent>()
            .HasIndex(e => e.TicketId);

        builder.Entity<WorkflowEvent>()
            .HasIndex(e => e.OccurredAt);

        builder.Entity<WorkflowEvent>()
            .HasIndex(e => e.EventType);

        // ErrorLog indexes
        builder.Entity<ErrorLog>()
            .HasIndex(e => e.TenantId);

        builder.Entity<ErrorLog>()
            .HasIndex(e => e.Severity);

        builder.Entity<ErrorLog>()
            .HasIndex(e => e.IsResolved);

        builder.Entity<ErrorLog>()
            .HasIndex(e => e.CreatedAt);

        builder.Entity<ErrorLog>()
            .HasIndex(e => new { e.EntityType, e.EntityId });

        builder.Entity<ErrorLog>()
            .HasIndex(e => new { e.TenantId, e.IsResolved, e.Severity });
    }

    private void ConfigureEnumConversions(ModelBuilder builder)
    {
        // Convert WorkflowState enum to string in database
        builder.Entity<Ticket>()
            .Property(t => t.State)
            .HasConversion(
                v => v.ToString(),
                v => (WorkflowState)Enum.Parse(typeof(WorkflowState), v)
            );

        // Also for WorkflowStateChanged events
        builder.Entity<WorkflowStateChanged>()
            .Property(e => e.From)
            .HasConversion(
                v => v.ToString(),
                v => (WorkflowState)Enum.Parse(typeof(WorkflowState), v)
            );

        builder.Entity<WorkflowStateChanged>()
            .Property(e => e.To)
            .HasConversion(
                v => v.ToString(),
                v => (WorkflowState)Enum.Parse(typeof(WorkflowState), v)
            );

        // ErrorLog enum conversion
        builder.Entity<ErrorLog>()
            .Property(e => e.Severity)
            .HasConversion(
                v => v.ToString(),
                v => (ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), v)
            );

        // QuestionAdded - store Question as JSON
        builder.Entity<QuestionAdded>()
            .OwnsOne(e => e.Question, question =>
            {
                question.ToJson();
                question.Ignore(q => q.Id); // Ignore Id - EF Core will use implicit ordinal key
                question.Property(q => q.Text).HasMaxLength(2000);
                question.Property(q => q.Category).HasMaxLength(100);
                question.Property(q => q.CreatedAt);
            });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update failed");
            throw;
        }
    }
}
