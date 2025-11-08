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

namespace PRFactory.Infrastructure.Persistence;

/// <summary>
/// Main Entity Framework Core DbContext for PRFactory.
/// Handles all database operations for Tenants, Repositories, and Tickets.
/// </summary>
public class ApplicationDbContext : DbContext
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new TenantConfig(_encryptionService));
        modelBuilder.ApplyConfiguration(new RepositoryConfig(_encryptionService));
        modelBuilder.ApplyConfiguration(new TicketConfig());
        modelBuilder.ApplyConfiguration(new TicketUpdateConfig());
        modelBuilder.ApplyConfiguration(new WorkflowEventConfig());
        modelBuilder.ApplyConfiguration(new WorkflowStateConfig());
        modelBuilder.ApplyConfiguration(new CheckpointConfig());
        modelBuilder.ApplyConfiguration(new AgentPromptTemplateConfig());

        // Add indexes for common queries
        AddIndexes(modelBuilder);

        // Configure enum conversions
        ConfigureEnumConversions(modelBuilder);
    }

    private void AddIndexes(ModelBuilder modelBuilder)
    {
        // Ticket indexes
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.TicketKey)
            .IsUnique();

        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.TenantId);

        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.RepositoryId);

        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.State);

        modelBuilder.Entity<Ticket>()
            .HasIndex(t => new { t.State, t.TenantId });

        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.CreatedAt);

        // Repository indexes
        modelBuilder.Entity<Repository>()
            .HasIndex(r => r.TenantId);

        modelBuilder.Entity<Repository>()
            .HasIndex(r => r.CloneUrl)
            .IsUnique();

        modelBuilder.Entity<Repository>()
            .HasIndex(r => r.GitPlatform);

        // Tenant indexes
        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Name)
            .IsUnique();

        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.IsActive);

        // WorkflowEvent indexes
        modelBuilder.Entity<WorkflowEvent>()
            .HasIndex(e => e.TicketId);

        modelBuilder.Entity<WorkflowEvent>()
            .HasIndex(e => e.OccurredAt);

        modelBuilder.Entity<WorkflowEvent>()
            .HasIndex(e => e.EventType);
    }

    private void ConfigureEnumConversions(ModelBuilder modelBuilder)
    {
        // Convert WorkflowState enum to string in database
        modelBuilder.Entity<Ticket>()
            .Property(t => t.State)
            .HasConversion(
                v => v.ToString(),
                v => (WorkflowState)Enum.Parse(typeof(WorkflowState), v)
            );

        // Also for WorkflowStateChanged events
        modelBuilder.Entity<WorkflowStateChanged>()
            .Property(e => e.From)
            .HasConversion(
                v => v.ToString(),
                v => (WorkflowState)Enum.Parse(typeof(WorkflowState), v)
            );

        modelBuilder.Entity<WorkflowStateChanged>()
            .Property(e => e.To)
            .HasConversion(
                v => v.ToString(),
                v => (WorkflowState)Enum.Parse(typeof(WorkflowState), v)
            );
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
