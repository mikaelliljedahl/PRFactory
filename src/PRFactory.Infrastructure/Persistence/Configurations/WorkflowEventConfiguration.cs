using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;
using PRFactory.Domain.ValueObjects;
using PRFactory.Infrastructure.Events;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for WorkflowEvent entities.
/// Uses Table-Per-Hierarchy (TPH) inheritance strategy.
/// </summary>
public class WorkflowEventConfiguration : IEntityTypeConfiguration<WorkflowEvent>
{
    public void Configure(EntityTypeBuilder<WorkflowEvent> builder)
    {
        builder.ToTable("WorkflowEvents");

        // Primary Key
        builder.HasKey(e => e.Id);

        // Discriminator for TPH inheritance
        builder.HasDiscriminator<string>("EventType")
            .HasValue<WorkflowStateChanged>(nameof(WorkflowStateChanged))
            .HasValue<QuestionAdded>(nameof(QuestionAdded))
            .HasValue<AnswerAdded>(nameof(AnswerAdded))
            .HasValue<PlanCreated>(nameof(PlanCreated))
            .HasValue<PullRequestCreated>(nameof(PullRequestCreated))
            .HasValue<WorkflowSuspended>(nameof(WorkflowSuspended))
            .HasValue<WorkflowCompleted>(nameof(WorkflowCompleted))
            .HasValue<WorkflowFailed>(nameof(WorkflowFailed))
            .HasValue<WorkflowCancelled>(nameof(WorkflowCancelled));

        // Common properties
        builder.Property(e => e.TicketId)
            .IsRequired();

        builder.Property(e => e.OccurredAt)
            .IsRequired();

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(100);

        // WorkflowStateChanged specific properties
        builder.HasDiscriminator()
            .HasValue<WorkflowStateChanged>(nameof(WorkflowStateChanged));

        // Configure derived types
        ConfigureWorkflowStateChanged(builder);
        ConfigureQuestionAdded(builder);
        ConfigureAnswerAdded(builder);
        ConfigurePlanCreated(builder);
        ConfigurePullRequestCreated(builder);
        ConfigureWorkflowSuspended(builder);
        ConfigureWorkflowCompleted(builder);
        ConfigureWorkflowFailed(builder);
        ConfigureWorkflowCancelled(builder);
    }

    private void ConfigureWorkflowStateChanged(EntityTypeBuilder<WorkflowEvent> builder)
    {
        // No additional configuration needed - From, To, and Reason are real properties
        // on WorkflowStateChanged class, not shadow properties
        // EF Core will discover them automatically through inheritance
    }

    private void ConfigureQuestionAdded(EntityTypeBuilder<WorkflowEvent> builder)
    {
        // QuestionAdded stores the question as a JSON complex type
        // This is handled by a separate configuration for the QuestionAdded type
        // No shadow properties needed - Question property exists on QuestionAdded class
    }

    private void ConfigureAnswerAdded(EntityTypeBuilder<WorkflowEvent> builder)
    {
        builder.Property<string>("QuestionId")
            .HasMaxLength(50);

        builder.Property<string>("AnswerText")
            .HasMaxLength(5000);
    }

    private void ConfigurePlanCreated(EntityTypeBuilder<WorkflowEvent> builder)
    {
        builder.Property<string>("BranchName")
            .HasMaxLength(200);
    }

    private void ConfigurePullRequestCreated(EntityTypeBuilder<WorkflowEvent> builder)
    {
        builder.Property<string>("PullRequestUrl")
            .HasMaxLength(1000);

        builder.Property<int>("PullRequestNumber");
    }

    private void ConfigureWorkflowSuspended(EntityTypeBuilder<WorkflowEvent> builder)
    {
        builder.Property<string>("GraphId")
            .HasMaxLength(100);

        builder.Property<string>("State")
            .HasMaxLength(200);
    }

    private void ConfigureWorkflowCompleted(EntityTypeBuilder<WorkflowEvent> builder)
    {
        builder.Property<TimeSpan>("Duration");
    }

    private void ConfigureWorkflowFailed(EntityTypeBuilder<WorkflowEvent> builder)
    {
        builder.Property<string>("GraphId")
            .HasMaxLength(100);

        builder.Property<string?>("Error")
            .HasMaxLength(2000);
    }

    private void ConfigureWorkflowCancelled(EntityTypeBuilder<WorkflowEvent> builder)
    {
        // WorkflowCancelled has no additional properties beyond base WorkflowEvent
    }
}
