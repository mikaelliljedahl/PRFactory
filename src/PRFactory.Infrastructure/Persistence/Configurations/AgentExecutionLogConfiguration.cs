using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the AgentExecutionLog entity.
/// </summary>
public class AgentExecutionLogConfiguration : IEntityTypeConfiguration<AgentExecutionLog>
{
    public void Configure(EntityTypeBuilder<AgentExecutionLog> builder)
    {
        builder.ToTable("AgentExecutionLogs");

        // Primary Key
        builder.HasKey(a => a.Id);

        // Properties
        builder.Property(a => a.TenantId)
            .IsRequired();

        builder.Property(a => a.TicketId)
            .IsRequired();

        builder.Property(a => a.AgentName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.ToolName)
            .HasMaxLength(100);

        builder.Property(a => a.Input)
            .IsRequired()
            .HasColumnType("TEXT"); // JSON serialized

        builder.Property(a => a.Output)
            .IsRequired()
            .HasColumnType("TEXT"); // JSON serialized

        builder.Property(a => a.Success)
            .IsRequired();

        builder.Property(a => a.ErrorMessage)
            .HasColumnType("TEXT");

        builder.Property(a => a.Duration)
            .IsRequired();

        builder.Property(a => a.TokensUsed);

        builder.Property(a => a.ExecutedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(a => a.TenantId)
            .HasDatabaseName("IX_AgentExecutionLogs_TenantId");

        builder.HasIndex(a => a.TicketId)
            .HasDatabaseName("IX_AgentExecutionLogs_TicketId");

        builder.HasIndex(a => a.ExecutedAt)
            .HasDatabaseName("IX_AgentExecutionLogs_ExecutedAt");

        // Composite index for agent-specific queries
        builder.HasIndex(a => new { a.TenantId, a.AgentName })
            .HasDatabaseName("IX_AgentExecutionLogs_TenantId_AgentName");

        // Composite index for ticket-level queries
        builder.HasIndex(a => new { a.TicketId, a.ExecutedAt })
            .HasDatabaseName("IX_AgentExecutionLogs_TicketId_ExecutedAt");

        // Relationships
        builder.HasOne(a => a.Tenant)
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Cascade); // Delete logs when tenant is deleted

        builder.HasOne(a => a.Ticket)
            .WithMany()
            .HasForeignKey(a => a.TicketId)
            .OnDelete(DeleteBehavior.Cascade); // Delete logs when ticket is deleted
    }
}
