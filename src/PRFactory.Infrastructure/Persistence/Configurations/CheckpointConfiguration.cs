using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the Checkpoint entity.
/// </summary>
public class CheckpointConfiguration : IEntityTypeConfiguration<Checkpoint>
{
    public void Configure(EntityTypeBuilder<Checkpoint> builder)
    {
        builder.ToTable("Checkpoints");

        // Primary Key
        builder.HasKey(c => c.Id);

        // Properties
        builder.Property(c => c.CheckpointId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.TenantId)
            .IsRequired();

        builder.Property(c => c.TicketId)
            .IsRequired();

        builder.Property(c => c.GraphId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.AgentName)
            .HasMaxLength(200);

        builder.Property(c => c.NextAgentType)
            .HasMaxLength(200);

        builder.Property(c => c.StateJson)
            .IsRequired()
            .HasColumnType("TEXT"); // SQLite uses TEXT for large strings

        builder.Property(c => c.AgentThreadId)
            .HasMaxLength(200);

        builder.Property(c => c.ConversationHistory)
            .HasColumnType("TEXT"); // JSON or compressed conversation history

        builder.Property(c => c.AgentState)
            .HasColumnType("TEXT"); // JSON serialized agent state

        builder.Property(c => c.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion(
                v => v.ToString(),
                v => (CheckpointStatus)Enum.Parse(typeof(CheckpointStatus), v)
            );

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt);

        builder.Property(c => c.ResumedAt);

        // Indexes
        // Index on TenantId for tenant-specific queries
        builder.HasIndex(c => c.TenantId)
            .HasDatabaseName("IX_Checkpoints_TenantId");

        // Index on TicketId for ticket-specific queries
        builder.HasIndex(c => c.TicketId)
            .HasDatabaseName("IX_Checkpoints_TicketId");

        // Composite index on TicketId + GraphId for getting latest checkpoint
        builder.HasIndex(c => new { c.TicketId, c.GraphId })
            .HasDatabaseName("IX_Checkpoints_TicketId_GraphId");

        // Composite index on TicketId + GraphId + Status for active checkpoint queries
        builder.HasIndex(c => new { c.TicketId, c.GraphId, c.Status })
            .HasDatabaseName("IX_Checkpoints_TicketId_GraphId_Status");

        // Index on Status for cleanup operations
        builder.HasIndex(c => c.Status)
            .HasDatabaseName("IX_Checkpoints_Status");

        // Index on CreatedAt for expiring old checkpoints
        builder.HasIndex(c => c.CreatedAt)
            .HasDatabaseName("IX_Checkpoints_CreatedAt");

        // Composite index on Status + CreatedAt for efficient expiration queries
        builder.HasIndex(c => new { c.Status, c.CreatedAt })
            .HasDatabaseName("IX_Checkpoints_Status_CreatedAt");

        // Relationships
        builder.HasOne(c => c.Ticket)
            .WithMany()
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Cascade); // Delete checkpoints when ticket is deleted

        builder.HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict); // Don't delete tenant if checkpoints exist
    }
}
