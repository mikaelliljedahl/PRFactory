using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRFactory.Domain.Entities;
using System.Text.Json;

namespace PRFactory.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the Ticket entity.
/// </summary>
public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        // Primary Key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.TicketKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.TicketSystem)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.TenantId)
            .IsRequired();

        builder.Property(t => t.RepositoryId)
            .IsRequired();

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.Description)
            .HasMaxLength(10000);

        builder.Property(t => t.State)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt);

        builder.Property(t => t.CompletedAt);

        // Collections stored as JSON
        builder.OwnsMany(t => t.Questions, question =>
        {
            question.ToJson();
            question.Ignore(q => q.Id); // Ignore Id - EF Core will use implicit ordinal key
            question.Property(q => q.Text).HasMaxLength(2000);
            question.Property(q => q.Category).HasMaxLength(100);
            question.Property(q => q.CreatedAt);
        });

        builder.OwnsMany(t => t.Answers, answer =>
        {
            answer.ToJson();
            answer.Property(a => a.QuestionId).HasMaxLength(50);
            answer.Property(a => a.Text).HasMaxLength(5000);
            answer.Property(a => a.AnsweredAt);
        });

        // Plan phase properties
        builder.Property(t => t.PlanBranchName)
            .HasMaxLength(200);

        builder.Property(t => t.PlanMarkdownPath)
            .HasMaxLength(500);

        builder.Property(t => t.PlanApprovedAt);

        // Implementation phase properties
        builder.Property(t => t.ImplementationBranchName)
            .HasMaxLength(200);

        builder.Property(t => t.PullRequestUrl)
            .HasMaxLength(1000);

        builder.Property(t => t.PullRequestNumber);

        // Error tracking
        builder.Property(t => t.RetryCount)
            .IsRequired();

        builder.Property(t => t.LastError)
            .HasMaxLength(2000);

        // Metadata stored as JSON
        builder.OwnsOne(t => t.Metadata, metadata =>
        {
            metadata.ToJson();
        });

        // Team review properties
        builder.Property(t => t.RequiredApprovalCount)
            .IsRequired()
            .HasDefaultValue(1);

        // Relationships
        builder.HasOne(t => t.Repository)
            .WithMany(r => r.Tickets)
            .HasForeignKey(t => t.RepositoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Tenant)
            .WithMany(tenant => tenant.Tickets)
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Events)
            .WithOne()
            .HasForeignKey(e => e.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.PlanReviews)
            .WithOne(pr => pr.Ticket)
            .HasForeignKey(pr => pr.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.ReviewComments)
            .WithOne(rc => rc.Ticket)
            .HasForeignKey(rc => rc.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
