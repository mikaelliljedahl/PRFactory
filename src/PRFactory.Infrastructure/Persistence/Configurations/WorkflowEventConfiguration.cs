using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;

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
            .HasValue<PullRequestCreated>(nameof(PullRequestCreated));

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
    }

    private void ConfigureWorkflowStateChanged(EntityTypeBuilder<WorkflowEvent> builder)
    {
        // Properties specific to WorkflowStateChanged
        builder.Property<string>("From")
            .HasMaxLength(50);

        builder.Property<string>("To")
            .HasMaxLength(50);

        builder.Property<string?>("Reason")
            .HasMaxLength(1000);
    }

    private void ConfigureQuestionAdded(EntityTypeBuilder<WorkflowEvent> builder)
    {
        // QuestionAdded stores the question as a JSON complex type
        // The Question property will be mapped through the entity's own configuration
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
}
