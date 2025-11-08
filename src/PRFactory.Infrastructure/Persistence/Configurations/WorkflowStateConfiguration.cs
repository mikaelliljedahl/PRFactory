using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Infrastructure.Persistence.Entities;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for WorkflowStateEntity.
/// </summary>
public class WorkflowStateConfiguration : IEntityTypeConfiguration<WorkflowStateEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowStateEntity> builder)
    {
        builder.ToTable("WorkflowStates");

        // Primary Key
        builder.HasKey(w => w.Id);

        // Properties
        builder.Property(w => w.WorkflowId)
            .IsRequired();

        builder.Property(w => w.TicketId)
            .IsRequired();

        builder.Property(w => w.CurrentGraph)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(w => w.CurrentState)
            .HasMaxLength(200);

        builder.Property(w => w.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(w => w.StartedAt)
            .IsRequired();

        builder.Property(w => w.CompletedAt);

        builder.Property(w => w.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(w => w.CreatedAt)
            .IsRequired();

        builder.Property(w => w.UpdatedAt)
            .IsRequired();

        // Indexes
        // Unique index on WorkflowId for fast lookup
        builder.HasIndex(w => w.WorkflowId)
            .IsUnique();

        // Index on TicketId for finding workflows by ticket
        builder.HasIndex(w => w.TicketId);

        // Index on Status for querying workflows by status
        builder.HasIndex(w => w.Status);

        // Composite index for common queries
        builder.HasIndex(w => new { w.TicketId, w.Status });
    }
}
